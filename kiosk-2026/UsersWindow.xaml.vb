Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Data
Imports System.Net
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports Npgsql

Public Class UsersWindow
    Inherits Window
    Implements INotifyPropertyChanged

    Private Const GWL_STYLE As Integer = -16
    Private Const WS_SYSMENU As Integer = &H80000

    ' ---------- User Model ----------
    Public Class User
        Implements INotifyPropertyChanged

        Private _username As String
        Private _fullname As String
        Private _phone As String
        Private _address As String
        Private _id_num As String
        Private _access_level As Boolean
        Private _view_reports As Boolean
        Private _edit_prod As Boolean
        Private _edit_prod_full As Boolean

        Public Property Uuid As String

        Public Property Username As String
            Get
                Return _username
            End Get
            Set(value As String)
                If _username <> value Then
                    _username = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        Public Property Fullname As String
            Get
                Return _fullname
            End Get
            Set(value As String)
                If _fullname <> value Then
                    _fullname = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        Public Property Phone As String
            Get
                Return _phone
            End Get
            Set(value As String)
                If _phone <> value Then
                    _phone = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        Public Property Address As String
            Get
                Return _address
            End Get
            Set(value As String)
                If _address <> value Then
                    _address = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        Public Property Id_num As String
            Get
                Return _id_num
            End Get
            Set(value As String)
                If _id_num <> value Then
                    _id_num = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        Public Property Access_level As Boolean
            Get
                Return _access_level
            End Get
            Set(value As Boolean)
                If _access_level <> value Then
                    _access_level = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        Public Property View_reports As Boolean
            Get
                Return _view_reports
            End Get
            Set(value As Boolean)
                If _view_reports <> value Then
                    _view_reports = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        ' Edit_prod property
        Public Property Edit_prod As Boolean
            Get
                Return _edit_prod
            End Get
            Set(value As Boolean)
                If _edit_prod <> value Then
                    _edit_prod = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        ' Edit_prod_full property
        Public Property Edit_prod_full As Boolean
            Get
                Return _edit_prod_full
            End Get
            Set(value As Boolean)
                If _edit_prod_full <> value Then
                    _edit_prod_full = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        Public Event PropertyChanged As PropertyChangedEventHandler _
            Implements INotifyPropertyChanged.PropertyChanged

        Protected Sub OnPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub
    End Class

    ' ---------- Properties for Binding ----------
    Private _users As ObservableCollection(Of User)
    Public Property Users As ObservableCollection(Of User)
        Get
            Return _users
        End Get
        Set(value As ObservableCollection(Of User))
            _users = value
            OnPropertyChanged()
        End Set
    End Property

    Private _selectedUser As User
    Public Property SelectedUser As User
        Get
            Return _selectedUser
        End Get
        Set(value As User)
            _selectedUser = value
            OnPropertyChanged()
        End Set
    End Property

    ' ---------- Constructor ----------
    Public Sub New()
        InitializeComponent()
        Users = New ObservableCollection(Of User)()
        Me.DataContext = Me
    End Sub

    ' ---------- Load Users ----------
    Private Async Sub UsersWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        txtBoxUsername.IsReadOnly = True
        Await FillUsersAsync()
    End Sub

    Private Async Function FillUsersAsync() As Task
        Dim WhoAmI As String = "FillUsersAsync"
        Dim sql As String = "SELECT
                                    uuid,
                                    username,
                                    fullname,
                                    phone,
                                    address,
                                    id_num,
                                    COALESCE(access_level, False) AS access_level,
                                    COALESCE(view_reports, FALSE) AS view_reports,
                                    COALESCE(edit_prod, FALSE) AS edit_prod,
                                    COALESCE(edit_prod_full, FALSE) AS edit_prod_full
                            FROM
                                    users WHERE kioskid = @kioskid AND deleted = FALSE"

        Try
            Users.Clear()

            Using conn = PostgresConnection.GetConnection()
                Await conn.OpenAsync()
                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)

                    Using dr As NpgsqlDataReader = Await cmd.ExecuteReaderAsync()
                        While Await dr.ReadAsync()
                            Users.Add(New User With {
                                .Uuid = dr("uuid").ToString(),
                                .Username = dr("username").ToString(),
                                .Fullname = dr("fullname").ToString(),
                                .Phone = dr("phone").ToString(),
                                .Address = dr("address").ToString(),
                                .Id_num = dr("id_num").ToString(),
                                .Access_level = Convert.ToBoolean(dr("access_level")),
                                .View_reports = Convert.ToBoolean(dr("view_reports")),
                                .Edit_prod = Convert.ToBoolean(dr("edit_prod")),
                                .Edit_prod_full = Convert.ToBoolean(dr("edit_prod_full"))
                            })
                        End While
                    End Using
                End Using
            End Using

        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", sql)
            MessageBox.Show(
                $"Error loading users: {ex.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            )
        End Try
    End Function

    ' ---------- Save or Update User ----------
    Public Async Function SaveUserAsync(user As User) As Task
        Dim WhoAmI As String = "SaveUserAsync"

        ' ----- Validation -----
        If String.IsNullOrEmpty(txtBoxFullname.Text) OrElse
       String.IsNullOrEmpty(txtBoxPhone.Text) OrElse
       String.IsNullOrEmpty(txtBoxAddress.Text) OrElse
       String.IsNullOrEmpty(txtBoxID.Text) OrElse
       String.IsNullOrEmpty(txtBoxUsername.Text) Then
            MessageBox.Show("Υπάρχουν κενά πεδία", "Πληροφορία", MessageBoxButton.OK, MessageBoxImage.Error)
            Exit Function
        End If

        Dim newPassword As String = passwordBox.Password.Trim()

        Try
            Using conn = PostgresConnection.GetConnection()
                Await conn.OpenAsync()
                Using cmd As New NpgsqlCommand()
                    cmd.Connection = conn
                    cmd.Parameters.Clear()

                    ' ----- New user -----
                    If String.IsNullOrEmpty(user.Uuid) Then

                        ' Check if username already exists
                        Dim checkSql As String = "SELECT COUNT(*) FROM users WHERE kioskid=@kioskid AND upper(username)=@username AND deleted=FALSE"
                        Using checkCmd As New NpgsqlCommand(checkSql, conn)
                            checkCmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)
                            checkCmd.Parameters.Add("@username", NpgsqlTypes.NpgsqlDbType.Varchar).Value = user.Username.ToUpper()

                            Dim count = Convert.ToInt32(Await checkCmd.ExecuteScalarAsync())
                            If count > 0 Then
                                MessageBox.Show("Το username υπάρχει ήδη.", "Σφάλμα", MessageBoxButton.OK, MessageBoxImage.Warning)
                                Exit Function
                            End If
                        End Using

                        ' Validate password
                        If String.IsNullOrEmpty(newPassword) Then
                            MessageBox.Show("Καταχωρήστε κωδικό", "Κενός κωδικός", MessageBoxButton.OK, MessageBoxImage.Warning)
                            Exit Function
                        End If
                        If Not IsNumeric(newPassword) Then
                            MessageBox.Show("Ο κωδικός πρέπει να αποτελείται μόνο από αριθμούς", "Μη έγκυρος κωδικός", MessageBoxButton.OK, MessageBoxImage.Warning)
                            Exit Function
                        End If

                        ' Insert new user
                        user.Uuid = Guid.NewGuid().ToString()
                        Dim sqlInsert As String = "INSERT INTO users
                                                (uuid, username, phone, pass, id_num, fullname, deleted, created_by, address, access_level, view_reports, edit_prod, edit_prod_full, kioskid)
                                               VALUES
                                                (@uuid, @username, @phone, @pass, @id_num, @fullname, @deleted, @created_by, @address, @access_level, @view_reports, @edit_prod, @edit_prod_full, @kioskid)"

                        cmd.CommandText = sqlInsert
                        cmd.Parameters.AddWithValue("@uuid", Guid.Parse(user.Uuid))
                        cmd.Parameters.AddWithValue("@username", user.Username)
                        cmd.Parameters.AddWithValue("@phone", user.Phone)
                        cmd.Parameters.AddWithValue("@pass", newPassword)
                        cmd.Parameters.AddWithValue("@id_num", user.Id_num)
                        cmd.Parameters.AddWithValue("@fullname", user.Fullname)
                        cmd.Parameters.AddWithValue("@deleted", False)
                        cmd.Parameters.AddWithValue("@created_by", currentUserID)
                        cmd.Parameters.AddWithValue("@address", user.Address)
                        cmd.Parameters.AddWithValue("@access_level", user.Access_level)
                        cmd.Parameters.AddWithValue("@view_reports", user.View_reports)
                        cmd.Parameters.AddWithValue("@edit_prod", user.Edit_prod)
                        cmd.Parameters.AddWithValue("@edit_prod_full", user.Edit_prod_full)
                        cmd.Parameters.AddWithValue("@kioskid", Guid.Parse(kioskid))

                        Await cmd.ExecuteNonQueryAsync()
                        Users.Add(user)
                        passwordBox.Clear()

                    Else
                        ' ----- Update existing user -----
                        Dim sqlUpdate As String = "UPDATE users SET 
                                                username=@username, 
                                                phone=@phone, 
                                                id_num=@id_num, 
                                                fullname=@fullname, 
                                                address=@address, 
                                                access_level=@access_level, 
                                                view_reports=@view_reports, 
                                                edit_prod=@edit_prod, 
                                                edit_prod_full=@edit_prod_full
                                               {0}
                                               WHERE uuid=@uuid"

                        ' Add password update only if a new password is entered
                        Dim passwordClause As String = ""
                        If Not String.IsNullOrEmpty(newPassword) Then
                            If Not IsNumeric(newPassword) Then
                                MessageBox.Show("Ο κωδικός πρέπει να αποτελείται μόνο από αριθμούς", "Μη έγκυρος κωδικός", MessageBoxButton.OK, MessageBoxImage.Warning)
                                Exit Function
                            End If
                            passwordClause = ", pass=@pass"
                            cmd.Parameters.AddWithValue("@pass", newPassword)
                        End If

                        cmd.CommandText = String.Format(sqlUpdate, passwordClause)
                        cmd.Parameters.AddWithValue("@uuid", Guid.Parse(user.Uuid))
                        cmd.Parameters.AddWithValue("@username", user.Username)
                        cmd.Parameters.AddWithValue("@phone", user.Phone)
                        cmd.Parameters.AddWithValue("@id_num", user.Id_num)
                        cmd.Parameters.AddWithValue("@fullname", user.Fullname)
                        cmd.Parameters.AddWithValue("@address", user.Address)
                        cmd.Parameters.AddWithValue("@access_level", user.Access_level)
                        cmd.Parameters.AddWithValue("@view_reports", user.View_reports)
                        cmd.Parameters.AddWithValue("@edit_prod", user.Edit_prod)
                        cmd.Parameters.AddWithValue("@edit_prod_full", user.Edit_prod_full)

                        Await cmd.ExecuteNonQueryAsync()
                        passwordBox.Clear()
                    End If
                End Using
            End Using
            txtBoxUsername.IsReadOnly = True
        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", "Save/Update User")
            MessageBox.Show($"Error saving user: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Function


    Public Async Function DeleteUserAsync(user As User) As Task
        Dim WhoAmI As String = "DeleteUserAsync"
        Dim sqlDelete As String = "UPDATE users SET deleted = TRUE, deleted_by = @deletedby WHERE kioskid = @kioskid AND uuid = @uuid"

        If MessageBox.Show("Να διαγραφεί ο χρήστης;", "Διαγραφή Χρήστη", MessageBoxButton.YesNo, MessageBoxImage.Question) = System.Windows.MessageBoxResult.Yes Then
            Try
                Using conn = PostgresConnection.GetConnection()
                    Await conn.OpenAsync()
                    Using cmd As New NpgsqlCommand()
                        cmd.Connection = conn
                        cmd.Parameters.Clear()

                        cmd.CommandText = sqlDelete
                        cmd.Parameters.AddWithValue("@uuid", Guid.Parse(user.Uuid))
                        cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)
                        cmd.Parameters.AddWithValue("@deletedby", currentUserID)
                        Await cmd.ExecuteNonQueryAsync()
                    End Using
                End Using
                Await FillUsersAsync()
                txtBoxUsername.IsReadOnly = True
            Catch ex As Exception
                CreateExceptionFile($"{WhoAmI}: {ex.Message}", "Delete User")
                MessageBox.Show(
                $"Error saving user: {ex.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            )
            End Try
        End If
    End Function


    ' ---------- INotifyPropertyChanged ----------
    Public Event PropertyChanged As PropertyChangedEventHandler _
        Implements INotifyPropertyChanged.PropertyChanged

    Protected Sub OnPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Private Async Sub SaveButton_Click(sender As Object, e As RoutedEventArgs)
        If SelectedUser IsNot Nothing Then
            Await SaveUserAsync(SelectedUser)
        End If
    End Sub

    Private Async Sub DeleteButton_Click(sender As Object, e As RoutedEventArgs)
        If SelectedUser IsNot Nothing Then
            Await DeleteUserAsync(SelectedUser)
        End If
    End Sub

    Private Sub NewButton_Click(sender As Object, e As RoutedEventArgs)
        txtBoxUsername.IsReadOnly = False
        SelectedUser = New User()
        MessageBox.Show("Συπληρώστε τα στοιχεία και πατήστε αποθήκευση",
                        "Νέος Χρήστης",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information)
    End Sub

    Private Sub ExitButton_Click(sender As Object, e As RoutedEventArgs)
        If AdminWin IsNot Nothing Then
            AdminWin.Show()
            AdminWin.Activate()
        End If
        Me.Close()
    End Sub

    <DllImport("user32.dll")>
    Private Shared Function GetWindowLong(hWnd As IntPtr, nIndex As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SetWindowLong(hWnd As IntPtr, nIndex As Integer, dwNewLong As Integer) As Integer
    End Function

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Dim hwnd As IntPtr = New WindowInteropHelper(Me).Handle
        Dim style As Integer = GetWindowLong(hwnd, GWL_STYLE)
        SetWindowLong(hwnd, GWL_STYLE, style And Not WS_SYSMENU)
    End Sub
End Class
