Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Data
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text.Encodings
Imports System.Windows.Interop
Imports Npgsql

Public Class SuppliersWindow
    Inherits Window
    Implements INotifyPropertyChanged

    Private Const GWL_STYLE As Integer = -16
    Private Const WS_SYSMENU As Integer = &H80000

    ' ---------- Supplier Model ----------
    Public Class Supplier
        Implements INotifyPropertyChanged

        Public Property Uuid As String
        Private _name As String
        Private _phone_1 As String
        Private _phone_2 As String
        Private _email As String
        Private _contact_name As String
        Private _mon As Int32
        Private _tue As Int32
        Private _wed As Int32
        Private _thu As Int32
        Private _fri As Int32
        Private _notes As String
        Private _is_default As Int32

        Public Property Name As String
            Get
                Return _name
            End Get
            Set(value As String)
                If _name <> value Then
                    _name = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        Public Property Phone_1 As String
            Get
                Return _phone_1
            End Get
            Set(value As String)
                If _phone_1 <> value Then
                    _phone_1 = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        Public Property Phone_2 As String
            Get
                Return _phone_2
            End Get
            Set(value As String)
                If _phone_2 <> value Then
                    _phone_2 = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        Public Property Email As String
            Get
                Return _email
            End Get
            Set(value As String)
                If _email <> value Then
                    _email = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        Public Property Mon As Int32
            Get
                Return _mon
            End Get
            Set(value As Int32)
                If _mon <> value Then
                    _mon = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        Public Property Tue As Int32
            Get
                Return _tue
            End Get
            Set(value As Int32)
                If _tue <> value Then
                    _tue = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        Public Property Wed As Int32
            Get
                Return _wed
            End Get
            Set(value As Int32)
                If _wed <> value Then
                    _wed = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        Public Property Thu As Int32
            Get
                Return _thu
            End Get
            Set(value As Int32)
                If _thu <> value Then
                    _thu = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        Public Property Fri As Int32
            Get
                Return _fri
            End Get
            Set(value As Int32)
                If _fri <> value Then
                    _fri = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        Public Property IsDefault As Int32
            Get
                Return _is_default
            End Get
            Set(value As Int32)
                If _is_default <> value Then
                    _is_default = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        Public Property ContactName As String
            Get
                Return _contact_name
            End Get
            Set(value As String)
                If _contact_name <> value Then
                    _contact_name = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        Public Property Notes As String
            Get
                Return _notes
            End Get
            Set(value As String)
                If _notes <> value Then
                    _notes = value
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
    Private _suppliers As ObservableCollection(Of Supplier)
    Public Property Suppliers As ObservableCollection(Of Supplier)
        Get
            Return _suppliers
        End Get
        Set(value As ObservableCollection(Of Supplier))
            _suppliers = value
            OnPropertyChanged()
        End Set
    End Property

    Private _selectedSupplier As Supplier
    Public Property SelectedSupplier As Supplier
        Get
            Return _selectedSupplier
        End Get
        Set(value As Supplier)
            _selectedSupplier = value
            OnPropertyChanged()
        End Set
    End Property

    ' ---------- Constructor ----------
    Public Sub New()
        InitializeComponent()
        Suppliers = New ObservableCollection(Of Supplier)()
        Me.DataContext = Me
    End Sub

    ' ---------- Load Suppliers ----------
    Private Async Sub SuppliersWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        txtBoxName.IsEnabled = False
        Await FillSuppliersAsync()
    End Sub

    Private Async Function FillSuppliersAsync() As Task
        Dim WhoAmI As String = "FillSuppliersAsync"
        txtBoxName.IsEnabled = False
        Dim sql As String =
        "SELECT
            uuid,
            s_name,
            COALESCE(phone_1, ' ') AS phone_1,
            COALESCE(phone_2, ' ') AS phone_2,
            COALESCE(email, ' ') AS email,
            COALESCE(contact_name, ' ') AS contact_name,
            COALESCE(mon, 0) AS mon,
            COALESCE(tue, 0) AS tue,
            COALESCE(wed, 0) AS wed,
            COALESCE(thu, 0) AS thu,
            COALESCE(fri, 0) AS fri,
            COALESCE(notes, ' ') AS notes,
            COALESCE(isdefault, 0) AS isdefault
        FROM suppliers
        WHERE kioskid = @kioskid
        ORDER BY s_name ASC;"

        Try
            Suppliers.Clear()

            Using conn = PostgresConnection.GetConnection()
                Await conn.OpenAsync()

                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value =
                    Guid.Parse(kioskid)

                    Using dr As NpgsqlDataReader = Await cmd.ExecuteReaderAsync()
                        While Await dr.ReadAsync()
                            Suppliers.Add(New Supplier With {
                            .Uuid = dr("uuid").ToString(),
                            .Name = dr("s_name").ToString(),
                            .Phone_1 = dr("phone_1").ToString(),
                            .Phone_2 = dr("phone_2").ToString(),
                            .Email = dr("email").ToString(),
                            .ContactName = dr("contact_name").ToString(),
                            .Mon = Convert.ToInt32(dr("mon")),
                            .Tue = Convert.ToInt32(dr("tue")),
                            .Wed = Convert.ToInt32(dr("wed")),
                            .Thu = Convert.ToInt32(dr("thu")),
                            .Fri = Convert.ToInt32(dr("fri")),
                            .Notes = dr("notes").ToString(),
                            .IsDefault = Convert.ToInt32(dr("isdefault"))
                        })
                        End While
                    End Using
                End Using
            End Using

        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", sql)
            MessageBox.Show(
            $"Error loading suppliers: {ex.Message}",
            "Database Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        )
        End Try
    End Function

    ' ---------- Save or Update Supplier ----------
    Public Async Function SaveSupplierAsync(supplier As Supplier) As Task
        Dim WhoAmI As String = "SaveSupplierAsync"

        ' ---------- Validation ----------
        If String.IsNullOrWhiteSpace(supplier.ContactName) Then
            MessageBox.Show("Το πεδίο όνομα επικοινωνίας δεν μπορεί να είναι κενό", "Πληροφορία", MessageBoxButton.OK, MessageBoxImage.Error)
            Return
        End If

        If Not String.IsNullOrWhiteSpace(supplier.Phone_1) AndAlso
       Not supplier.Phone_1.All(AddressOf Char.IsDigit) Then
            MessageBox.Show("Το πεδίο τηλέφωνο (1) πρέπει να αποτελείται μόνο από αριθμούς", "Πληροφορία", MessageBoxButton.OK, MessageBoxImage.Error)
            Return
        End If

        If Not String.IsNullOrWhiteSpace(supplier.Phone_2) AndAlso Not supplier.Phone_2.All(AddressOf Char.IsDigit) Then
            MessageBox.Show("Το πεδίο τηλέφωνο (2) πρέπει να αποτελείται μόνο από αριθμούς", "Πληροφορία", MessageBoxButton.OK, MessageBoxImage.Error)
            Return
        End If

        ' ---------- SQL ----------
        Dim sqlInsert As String =
        "INSERT INTO suppliers
        (uuid, kioskid, s_name, phone_1, phone_2, email, contact_name,
         mon, tue, wed, thu, fri, notes)
         VALUES
        (@uuid, @kioskid, @name, @phone1, @phone2, @email, @contact,
         @mon, @tue, @wed, @thu, @fri, @notes);"

        Dim sqlUpdate As String =
        "UPDATE suppliers SET
            s_name = @name,
            phone_1 = @phone1,
            phone_2 = @phone2,
            email = @email,
            contact_name = @contact,
            mon = @mon,
            tue = @tue,
            wed = @wed,
            thu = @thu,
            fri = @fri,
            notes = @notes
         WHERE kioskid = @kioskid AND uuid = @uuid;"

        Try
            Using conn = PostgresConnection.GetConnection()
                Await conn.OpenAsync()

                Using cmd As New NpgsqlCommand()
                    cmd.Connection = conn

                    ' ---------- Common parameters ----------
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value =
                    Guid.Parse(kioskid)

                    cmd.Parameters.AddWithValue("@uuid",
                    If(String.IsNullOrEmpty(supplier.Uuid),
                       Guid.NewGuid(),
                       Guid.Parse(supplier.Uuid)))

                    cmd.Parameters.AddWithValue("@kioskid", Guid.Parse(kioskid))
                    cmd.Parameters.AddWithValue("@name", supplier.Name)
                    cmd.Parameters.Add("@phone1", NpgsqlTypes.NpgsqlDbType.Text).Value = If(String.IsNullOrWhiteSpace(supplier.Phone_1), DBNull.Value, supplier.Phone_1)
                    cmd.Parameters.Add("@phone2", NpgsqlTypes.NpgsqlDbType.Text).Value = If(String.IsNullOrWhiteSpace(supplier.Phone_2), DBNull.Value, supplier.Phone_2)
                    cmd.Parameters.Add("@email", NpgsqlTypes.NpgsqlDbType.Text).Value = If(String.IsNullOrWhiteSpace(supplier.Email), DBNull.Value, supplier.Email)
                    cmd.Parameters.AddWithValue("@contact", supplier.ContactName)
                    cmd.Parameters.AddWithValue("@mon", supplier.Mon)
                    cmd.Parameters.AddWithValue("@tue", supplier.Tue)
                    cmd.Parameters.AddWithValue("@wed", supplier.Wed)
                    cmd.Parameters.AddWithValue("@thu", supplier.Thu)
                    cmd.Parameters.AddWithValue("@fri", supplier.Fri)
                    cmd.Parameters.Add("@notes", NpgsqlTypes.NpgsqlDbType.Text).Value = If(String.IsNullOrWhiteSpace(supplier.Notes), DBNull.Value, supplier.Notes)


                    ' ---------- Insert / Update ----------
                    If String.IsNullOrEmpty(supplier.Uuid) Then

                        If supplierExists(supplier.Name) Then
                            MessageBox.Show("Υπάρχει ήδη καταχωρημένος Προμηθευτής με αυτό το όνομα", "Πληροφορία", MessageBoxButton.OK, MessageBoxImage.Error)
                            Return
                        End If

                        supplier.Uuid = cmd.Parameters("@uuid").Value.ToString()
                        cmd.CommandText = sqlInsert
                        Await cmd.ExecuteNonQueryAsync()

                        Suppliers.Add(supplier)
                    Else
                        cmd.CommandText = sqlUpdate
                        Await cmd.ExecuteNonQueryAsync()
                    End If
                End Using
            End Using
            txtBoxName.IsEnabled = False
            MessageBox.Show("Η εντολή εκτελέστηκε επιτυχώς",
                    "Πληροφορία",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information)
        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", "SaveSupplier")
            MessageBox.Show($"Error saving supplier: {ex.Message}",
                        "Database Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error)
        End Try
    End Function


    Private Function SupplierExists(name As String) As Boolean
        Dim sql As String =
        "SELECT COUNT(*) 
         FROM suppliers 
         WHERE kioskid = @kioskid AND UPPER(s_name) = UPPER(@name);"

        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New Npgsql.NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value =
                    Guid.Parse(kioskid)
                    cmd.Parameters.AddWithValue("@name", name)
                    Dim count As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                    Return count > 0
                End Using
            End Using

        Catch ex As Exception
            CreateExceptionFile(ex.Message, sql)
            MessageBox.Show(ex.Message,
                        "Application Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error)
            Return False
        End Try
    End Function

    ' ---------- INotifyPropertyChanged ----------
    Public Event PropertyChanged As PropertyChangedEventHandler _
        Implements INotifyPropertyChanged.PropertyChanged

    Protected Sub OnPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Private Async Sub SaveButton_Click(sender As Object, e As RoutedEventArgs)
        If SelectedSupplier IsNot Nothing Then
            Await SaveSupplierAsync(SelectedSupplier)
        End If
    End Sub

    Private Sub NewButton_Click(sender As Object, e As RoutedEventArgs)
        SelectedSupplier = New Supplier()
        txtBoxName.IsEnabled = True
        MessageBox.Show("Συπληρώστε τα στοιχεία και πατήστε αποθήκευση",
                        "Νέος Προμηθευτής",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information)
    End Sub

    Private Async Sub DeleteButton_Click(sender As Object, e As EventArgs)
        If SelectedSupplier IsNot Nothing Then
            Await DeleteSupplierAsync(SelectedSupplier)
        End If
        End Sub

    Public Async Function DeleteSupplierAsync(supplier As Supplier) As Task
        Dim WhoAmI As String = "DeleteSupplierAsync"

        If supplier.IsDefault = 1 Then
            MessageBox.Show("Δεν μπορείτε να διαγράψετε αυτόν τον προμηθευτή",
                        "Διαγραφή Προμηθευτή",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error)
            Return
        End If

        Dim sqlUpdateProducts As String =
        "UPDATE products
         SET supplier_id = (
             SELECT uuid FROM suppliers
             WHERE kioskid = @kioskid AND isdefault = 1 LIMIT 1
         )
         WHERE kioskid = @kioskid AND supplier_id = @supplierId;"

        Dim sqlDeleteSupplier As String =
        "DELETE FROM suppliers
         WHERE kioskid = @kioskid AND uuid = @supplierId;"

        Using conn = PostgresConnection.GetConnection()
            Await conn.OpenAsync()

            Using tran = conn.BeginTransaction()   ' NO Await here
                Try
                    Using cmd As New Npgsql.NpgsqlCommand(sqlUpdateProducts, conn, tran)
                        cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)
                        cmd.Parameters.Add("@supplierId", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(supplier.Uuid)
                        Await cmd.ExecuteNonQueryAsync()
                    End Using

                    Using cmd As New Npgsql.NpgsqlCommand(sqlDeleteSupplier, conn, tran)
                        cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)
                        cmd.Parameters.Add("@supplierId", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(supplier.Uuid)
                        Await cmd.ExecuteNonQueryAsync()
                    End Using

                    tran.Commit()

                Catch ex As Exception
                    tran.Rollback()
                    CreateExceptionFile(ex.Message, WhoAmI)
                    Throw
                End Try
            End Using
        End Using

        MessageBox.Show("Η εντολή εκτελέστηκε επιτυχώς",
                    "Πληροφορία",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information)

        Await FillSuppliersAsync()
    End Function


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
