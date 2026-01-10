Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Data
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports Npgsql

Public Class LotteryWindow
    Inherits Window


    Private Const GWL_STYLE As Integer = -16
    Private Const WS_SYSMENU As Integer = &H80000

    Private Sub LotteryWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        LoadLotteryDetails()
        CalculateAmount()
    End Sub

    Private Sub LoadLotteryDetails()
        Dim WhoAmI As String = "LoadLotteryDetails"
        Dim sql As String = ""

        dgvLinkedProducts.ItemsSource = Nothing
        txtBoxLotteryAmt.Text = ""

        Try
            sql = "
            SELECT 
                b.product_serno,
                l.barcode,
                p.description,
                p.sell_amt,
                p.avail_quantity
            FROM lottery l
            INNER JOIN barcodes b ON b.barcode = l.barcode
            INNER JOIN products p ON p.serno = b.product_serno
            WHERE p.kioskid = @kioskid
        "

            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value =
                    Guid.Parse(kioskid)

                    Using dr As NpgsqlDataReader = cmd.ExecuteReader()
                        Dim table As New DataTable()
                        table.Load(dr)
                        dgvLinkedProducts.ItemsSource = table.DefaultView
                    End Using
                End Using
            End Using

            ' Recalculate total after loading data
            CalculateAmount()

        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", sql)

            MessageBox.Show(
            $"Error loading lottery details:{Environment.NewLine}{ex.Message}",
            "Database Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub CalculateAmount()
        Dim totalAmt As Decimal = 0

        Dim view As DataView = TryCast(dgvLinkedProducts.ItemsSource, DataView)
        If view Is Nothing Then Exit Sub

        For Each row As DataRowView In view
            Dim sellAmt As Decimal = Convert.ToDecimal(row("sell_amt"))
            Dim availQnt As Integer = Convert.ToInt32(row("avail_quantity"))

            totalAmt += sellAmt * availQnt
        Next

        txtBoxLotteryAmt.Text = totalAmt.ToString("N2") & " ευρώ"
    End Sub

    Private Sub SaveButton_Click(sender As Object, e As RoutedEventArgs)
        Dim sql As String = ""
        Dim whoAmI As String = "SaveButton_Click"

        Dim view As DataView = TryCast(dgvLinkedProducts.ItemsSource, DataView)

        If view Is Nothing OrElse view.Count = 0 Then
            MessageBox.Show(
            "Δεν έχετε συνδέσει προϊόν(τα) με τα λαχεία",
            "Σφάλμα",
            MessageBoxButton.OK,
            MessageBoxImage.Error)
            Exit Sub
        End If

        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using tran = conn.BeginTransaction()

                    ' 1. Delete existing lottery records
                    sql = "DELETE FROM lottery"
                    Using deleteCmd As New NpgsqlCommand(sql, conn, tran)
                        deleteCmd.ExecuteNonQuery()
                    End Using

                    ' 2. Insert new records
                    sql = "INSERT INTO lottery (barcode) VALUES (@barcode)"

                    Using insertCmd As New NpgsqlCommand(sql, conn, tran)
                        insertCmd.Parameters.Add("@barcode", NpgsqlTypes.NpgsqlDbType.Text)

                        For Each row As DataRowView In view
                            insertCmd.Parameters("@barcode").Value = row("barcode").ToString()
                            insertCmd.ExecuteNonQuery()
                        Next
                    End Using

                    tran.Commit()
                End Using
            End Using

            CalculateAmount()
            ExitButton_Click(sender, e)

        Catch ex As Exception
            CreateExceptionFile($"{whoAmI}: {ex.Message}", sql)

            MessageBox.Show(
            $"Σφάλμα κατά την αποθήκευση:{Environment.NewLine}{ex.Message}",
            "Application Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error)
        End Try
    End Sub


    Private Sub NewButton_Click(sender As Object, e As RoutedEventArgs)
        Dim newBarcode As Object
        newBarcode = InputBox("Εισαγωγή νέου Barcode", "Νέο Barcode", "")
        If newBarcode Is "" Then
            Exit Sub
        Else
            addInGrid(newBarcode)
        End If
        txtBoxLotteryAmt.Focus()
    End Sub

    Private Sub AddInGrid(newBarcode As String)
        Dim sql As String = ""
        Dim whoAmI As String = "AddInGrid"

        Dim view As DataView = TryCast(dgvLinkedProducts.ItemsSource, DataView)
        If view Is Nothing Then
            view = New DataView(New DataTable())
            dgvLinkedProducts.ItemsSource = view
        End If

        Try
            sql = "
            SELECT 
                b.product_serno,
                b.barcode,
                p.description,
                p.sell_amt,
                p.avail_quantity
            FROM barcodes b
            INNER JOIN products p ON p.serno = b.product_serno
            WHERE p.kioskid = @kioskid AND UPPER(b.barcode) = @barcode                   
        "

            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value =
                    Guid.Parse(kioskid)
                    cmd.Parameters.Add("@barcode", NpgsqlTypes.NpgsqlDbType.Text).Value =
                    newBarcode.ToUpper()

                    Using dr As NpgsqlDataReader = cmd.ExecuteReader()
                        If dr.Read() Then

                            ' Check for duplicate product_serno
                            For Each row As DataRowView In view
                                If Convert.ToInt32(row("product_serno")) =
                               Convert.ToInt32(dr("product_serno")) Then

                                    MessageBox.Show(
                                    "Το προϊόν είναι ήδη συνδεμένο με το κουμπί",
                                    "Καταχώρηση Barcode/Προϊόντος",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Stop)
                                    Exit Sub
                                End If
                            Next

                            ' Add new row
                            Dim table As DataTable = view.Table
                            Dim newRow As DataRow = table.NewRow()

                            newRow("product_serno") = dr("product_serno")
                            newRow("barcode") = dr("barcode")
                            newRow("description") = dr("description")
                            newRow("sell_amt") = dr("sell_amt")
                            newRow("avail_quantity") = dr("avail_quantity")

                            table.Rows.Add(newRow)

                            CalculateAmount()
                        Else
                            MessageBox.Show(
                            "Το barcode δεν είναι καταχωρημένο στη διαχείριση προϊόντων",
                            "Σφάλμα",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error)
                        End If
                    End Using
                End Using
            End Using

        Catch ex As Exception
            CreateExceptionFile($"{whoAmI}: {ex.Message}", sql)
        End Try
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
