Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports Npgsql
Imports System.Threading.Tasks
Imports System.Windows

Public Class CategoriesWindow
    Inherits Window
    Implements INotifyPropertyChanged

    ' ---------- Category Model ----------
    Public Class Category
        Implements INotifyPropertyChanged

        Private _description As String
        Private _vat As Decimal

        Public Property Uuid As String

        Public Property Description As String
            Get
                Return _description
            End Get
            Set(value As String)
                If _description <> value Then
                    _description = value
                    OnPropertyChanged()
                End If
            End Set
        End Property

        Public Property Vat As Decimal
            Get
                Return _vat
            End Get
            Set(value As Decimal)
                If _vat <> value Then
                    _vat = value
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
    Private _categories As ObservableCollection(Of Category)
    Public Property Categories As ObservableCollection(Of Category)
        Get
            Return _categories
        End Get
        Set(value As ObservableCollection(Of Category))
            _categories = value
            OnPropertyChanged()
        End Set
    End Property

    Private _selectedCategory As Category
    Public Property SelectedCategory As Category
        Get
            Return _selectedCategory
        End Get
        Set(value As Category)
            _selectedCategory = value
            OnPropertyChanged()
        End Set
    End Property

    ' ---------- Constructor ----------
    Public Sub New()
        InitializeComponent()
        Categories = New ObservableCollection(Of Category)()
        Me.DataContext = Me
    End Sub

    ' ---------- Load Categories ----------
    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Await FillCategoriesAsync()
    End Sub

    Private Async Function FillCategoriesAsync() As Task
        Dim WhoAmI As String = "FillCategoriesAsync"
        Dim sql As String = "SELECT uuid, description, vat FROM categories WHERE kioskid = @kioskid"

        Try
            Categories.Clear()

            Using conn = PostgresConnection.GetConnection()
                Await conn.OpenAsync()
                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)

                    Using dr As NpgsqlDataReader = Await cmd.ExecuteReaderAsync()
                        While Await dr.ReadAsync()
                            Categories.Add(New Category With {
                                .Uuid = dr("uuid").ToString(),
                                .Description = dr("description").ToString(),
                                .Vat = Convert.ToDecimal(dr("vat"))
                            })
                        End While
                    End Using
                End Using
            End Using

        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", sql)
            MessageBox.Show(
                $"Error loading categories: {ex.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            )
        End Try
    End Function

    ' ---------- Save or Update Category ----------
    Public Async Function SaveCategoryAsync(category As Category) As Task
        Dim WhoAmI As String = "SaveCategoryAsync"

        ' ----- Validation -----
        If String.IsNullOrWhiteSpace(category.Description) Then
            MessageBox.Show("Υπάρχουν κενά πεδία στην Περιγραφή", "Πληροφορία", MessageBoxButton.OK, MessageBoxImage.Error)
            Return
        End If

        If category.Vat <> 0 AndAlso category.Vat <> 3 AndAlso category.Vat <> 5 AndAlso category.Vat <> 19 Then
            MessageBox.Show("Ο Φ.Π.Α. πρέπει να είναι 0% ή 3% ή 5% ή 19%", "Πληροφορία", MessageBoxButton.OK, MessageBoxImage.Error)
            Return
        End If

        Dim sqlInsert As String = "INSERT INTO categories (uuid, kioskid, description, vat) VALUES (@uuid, @kioskid, @description, @vat)"
        Dim sqlUpdate As String = "UPDATE categories SET description = @description, vat = @vat WHERE uuid = @uuid"

        Try
            Using conn = PostgresConnection.GetConnection()
                Await conn.OpenAsync()
                Using cmd As New NpgsqlCommand()
                    cmd.Connection = conn
                    cmd.Parameters.Clear()

                    If String.IsNullOrEmpty(category.Uuid) Then
                        ' ----- New category -----
                        category.Uuid = Guid.NewGuid().ToString()
                        cmd.CommandText = sqlInsert
                        cmd.Parameters.AddWithValue("@uuid", Guid.Parse(category.Uuid))
                        cmd.Parameters.AddWithValue("@kioskid", Guid.Parse(kioskid))
                        cmd.Parameters.AddWithValue("@description", category.Description)
                        cmd.Parameters.AddWithValue("@vat", category.Vat)
                        Await cmd.ExecuteNonQueryAsync()

                        ' Add to ObservableCollection so UI updates
                        Categories.Add(category)
                    Else
                        ' ----- Update existing category -----
                        cmd.CommandText = sqlUpdate
                        cmd.Parameters.AddWithValue("@uuid", Guid.Parse(category.Uuid))
                        cmd.Parameters.AddWithValue("@description", category.Description)
                        cmd.Parameters.AddWithValue("@vat", category.Vat)
                        Await cmd.ExecuteNonQueryAsync()
                    End If
                End Using
            End Using

        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", "Save/Update Category")
            MessageBox.Show(
            $"Error saving category: {ex.Message}",
            "Database Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        )
        End Try
    End Function


    ' ---------- INotifyPropertyChanged ----------
    Public Event PropertyChanged As PropertyChangedEventHandler _
        Implements INotifyPropertyChanged.PropertyChanged

    Protected Sub OnPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Private Async Sub SaveButton_Click(sender As Object, e As RoutedEventArgs)
        If SelectedCategory IsNot Nothing Then
            Await SaveCategoryAsync(SelectedCategory)
        End If
    End Sub

    Private Sub NewButton_Click(sender As Object, e As RoutedEventArgs)
        SelectedCategory = New Category()
    End Sub

    Private Sub ExitButton_Click(sender As Object, e As RoutedEventArgs)
        If AdminWin IsNot Nothing Then
            AdminWin.Show()
            AdminWin.Activate()
        End If
        Me.Close()
    End Sub

End Class
