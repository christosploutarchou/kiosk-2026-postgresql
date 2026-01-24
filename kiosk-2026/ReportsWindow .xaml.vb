Imports System.Data
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports kiosk_2026.SuppliersWindow
Imports Npgsql

Public Class ReportsWindow
    Inherits Window
    Private Const GWL_STYLE As Integer = -16
    Private Const WS_SYSMENU As Integer = &H80000

    Public Class UserItem
        Public Property Uuid As Guid
        Public Property Username As String
    End Class

    Public Class SupplierItem
        Public Property Uuid As Guid
        Public Property Name As String
        Public Property Phone1 As String
        Public Property Phone2 As String
    End Class

    Public Class CategoryItem
        Public Property Uuid As Guid
        Public Property Description As String
        Public Property Vat As Decimal
    End Class

    Public Class ReceiptReportItem
        Public Property DateFrom As DateTime
        Public Property DateTo As DateTime
        Public Property TotalAmt As Decimal
        Public Property Category As String
        Public Property Suppliers As String
    End Class

    Private Sub ReportsWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        rdbSalesPerProduct.IsChecked = True
        txtBoxBarcode.Focus()

        ' Put all radio buttons in one list
        Dim allRadioButtons = {
        rdbSalesPerProduct,
        rdbSalesPerVAT,
        rdbXReport,
        rdbZReport,
        rdbSalesPerCategory,
        rdbBuySellSupplier,
        rdbSupplierPr,
        rdbUsers,
        rdbPayments,
        rdbQntHistory,
        rdbSessions
    }

        ' Hide everything by default
        For Each rb In allRadioButtons
            rb.Visibility = Visibility.Collapsed
        Next

        ' Admins or users who can view reports see all
        If currentUser.isAdmin Or currentUser.canViewReports Then
            For Each rb In allRadioButtons
                rb.Visibility = Visibility.Visible
            Next
        End If

        ' Product editors get limited access
        If currentUser.canEditProducts Or currentUser.canEditProductsFull Then
            rdbSalesPerProduct.Visibility = Visibility.Visible
            rdbSupplierPr.Visibility = Visibility.Visible
            rdbSalesPerCategory.Visibility = Visibility.Visible
        End If
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

    Private Sub ReportRadio_Checked(sender As Object, e As RoutedEventArgs)
        Dim rb = DirectCast(sender, RadioButton)
        SetVisibleFields(CStr(rb.Tag))
    End Sub

    Private Sub SetVisibleFields(ByVal reportType As String)
        ClearGridAndSetInvisible()
        'lblTotalSalesAmount.Content = ""
        chkBoxSalesPerSupplier.IsChecked = False
        cmbCategories.Visibility = Visibility.Collapsed

        'lblBarcode.Visibility
        If {"QNT_HISTORY", "PAYMENTS", "SALES_PER_VAT", "Z_REPORT", "X_REPORT", "PRODUCTS_PER_SUPPLIER",
            "HOURS_PER_USER", "LOGIN_HISTORY", "SALES_PER_CATEGORY", "BUY_SELL"}.Contains(reportType) Then
            lblBarcode.Visibility = Visibility.Collapsed
            txtBoxBarcode.Visibility = Visibility.Collapsed
            btnClearBarcode.Visibility = Visibility.Collapsed
        Else
            lblBarcode.Visibility = Visibility.Visible
            txtBoxBarcode.Visibility = Visibility.Visible
            btnClearBarcode.Visibility = Visibility.Visible
        End If

        ' ---------- QNT HISTORY ----------
        If reportType = "QNT_HISTORY" Then
            'cmbNoBarcode.Visibility = Visibility.Collapsed
        End If

        ' ---------- PRODUCTS PER SUPPLIER ----------
        If reportType = "PRODUCTS_PER_SUPPLIER" Then
            chkBoxSalesPerSupplier.Visibility = Visibility.Visible
            cmbSupplier.Visibility = Visibility.Visible
            FillSuppliers()
        Else
            chkBoxSalesPerSupplier.Visibility = Visibility.Collapsed
            cmbSupplier.Visibility = Visibility.Collapsed
        End If

        ' ---------- PAYMENTS VAT ----------
        If (reportType = "PAYMENTS" AndAlso currentUser.isAdmin) Or reportType = "SALES_PER_PRODUCT" Then
            lblAmountVAT.Visibility = Visibility.Visible
        Else
            lblAmountVAT.Visibility = Visibility.Collapsed
            lblAmountVAT.Content = ""
        End If

        If {"PAYMENTS", "HOURS_PER_USER"}.Contains(reportType) Then
            txtBoxTotalHoursOrPayments.Visibility = Visibility.Visible
            txtBoxTotalHoursOrPayments.Text = "0"
            lblTotalHoursOrAmount.Visibility = Visibility.Visible
            lblTotalHoursOrAmount.Content = "Σύνολο"
        Else
            txtBoxTotalHoursOrPayments.Visibility = Visibility.Collapsed
            lblTotalHoursOrAmount.Visibility = Visibility.Collapsed
        End If

        ' ---------- SEARCH BUTTON ----------
        If {"QNT_HISTORY", "SALES_PER_VAT", "X_REPORT", "Z_REPORT",
        "HOURS_PER_USER", "PAYMENTS", "LOGIN_HISTORY", "SALES_PER_CATEGORY"}.Contains(reportType) Then
            btnSearch.Visibility = Visibility.Visible
        Else
            btnSearch.Visibility = Visibility.Collapsed
        End If

        ' ---------- DATE FIELDS ----------
        If {"QNT_HISTORY", "SALES_PER_PRODUCT", "SALES_PER_VAT", "X_REPORT",
        "Z_REPORT", "HOURS_PER_USER", "PAYMENTS", "LOGIN_HISTORY",
        "SALES_PER_CATEGORY"}.Contains(reportType) Then
            ShowDateFields(True)
        Else
            ShowDateFields(False)
        End If

        If {"HOURS_PER_USER", "X_REPORT"}.Contains(reportType) Then
            cmbUsers.Visibility = Visibility.Visible
        Else
            cmbUsers.Visibility = Visibility.Collapsed
        End If

        ' ---------- QUANTITY / BUY SELL ----------
        If reportType = "QUANTITY_PER_PRODUCT" OrElse reportType = "BUY_SELL" Then
            btnPrint.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed
            txtBoxBarcode.Focus()
        End If
        ' ---------- SALES PER PRODUCT ----------
        If reportType = "SALES_PER_PRODUCT" Then
            FillProductsNoBarcode()
            btnPrint.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Visible
            txtBoxBarcode.Focus()
            ' ---------- PAYMENTS ----------
        ElseIf reportType = "PAYMENTS" Then
            btnPrint.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed
            ' ---------- VAT / Z REPORT ----------
        ElseIf reportType = "SALES_PER_VAT" OrElse reportType = "Z_REPORT" Then
            btnPrint.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed
            ' ---------- X REPORT ----------
        ElseIf reportType = "X_REPORT" Then
            btnPrint.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed
            FillUsers(True)
            ' ---------- PRODUCTS PER SUPPLIER ----------
        ElseIf reportType = "PRODUCTS_PER_SUPPLIER" Then
            btnPrint.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed
            ' ---------- HOURS PER USER ----------
        ElseIf reportType = "HOURS_PER_USER" Then
            FillUsers(False)
            btnPrint.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed
            ' ---------- LOGIN HISTORY ----------
        ElseIf reportType = "LOGIN_HISTORY" Then
            btnPrint.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed
            ' ---------- SALES PER CATEGORY ----------
        ElseIf reportType = "SALES_PER_CATEGORY" Then
            btnPrint.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed
            cmbCategories.Visibility = Visibility.Visible
            FillCategories(1)
        End If
    End Sub

    Private Sub FillProductsNoBarcode()
        'cmbNoBarcode.Items.Clear()
        'cmbNoBarcode.Items.Add("")
        'cmbNoBarcode.Items.Add("Φ.Π.Α 5%")
        'cmbNoBarcode.Items.Add("Φ.Π.Α 19%")
    End Sub

    Private Sub FillSuppliers()
        Dim WhoAmI As String = "FillSuppliers"
        Dim sql As String =
        "SELECT uuid, s_name, phone_1, phone_2
         FROM suppliers
         WHERE kioskid = @kioskid
         ORDER BY s_name ASC"

        Try
            Dim suppliers As New List(Of SupplierItem)

            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).
                    Value = Guid.Parse(kioskid)

                    Using dr = cmd.ExecuteReader()
                        While dr.Read()
                            suppliers.Add(New SupplierItem With {
                            .Uuid = dr.GetGuid(0),
                            .Name = dr.GetString(1),
                            .Phone1 = If(dr.IsDBNull(2), "", dr.GetString(2)),
                            .Phone2 = If(dr.IsDBNull(3), "", dr.GetString(3))
                        })
                        End While
                    End Using
                End Using
            End Using

            cmbSupplier.ItemsSource = suppliers

        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", sql)
            MessageBox.Show(
                $"Error loading categories: {ex.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            )
        End Try
    End Sub

    Public Class SupplierReportItem
        Public Property Index As Integer
        Public Property Supplier As String
        Public Property Product As String
        Public Property Quantity As Integer
        Public Property BuyPrice As Decimal?
        Public Property SellPrice As Decimal
        Public Property Barcode As String
        Public Property AvailableQty As Integer
        Public Property StockQty As Integer
    End Class

    Private Sub cmbSupplier_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles cmbSupplier.SelectionChanged
        If cmbSupplier.SelectedItem Is Nothing Then Exit Sub
        ClearGridAndSetInvisible()

        Dim sql As String = "Select
    s.s_name || ' ' ||
    COALESCE(s.phone_1,'') || ' ' ||
    COALESCE(s.phone_2,'')               AS supplier,
    p.description                        As product,
    SUM(rd.quantity)                     As qty,
    COALESCE(p.buy_amt_no_vat, 0)         As buy_amt,
    COALESCE(p.sell_amt, 0)               As sell_amt,
    (
        Select Case b.barcode
        From barcodes b
        Where b.product_serno = rd.product_serno
            LIMIT 1
    )                                    AS barcode,
    rd.avail_quantity,
    rd.stock_quantity
From receipts_det rd
Join products p
    On p.serno = rd.product_serno
Join Suppliers s
    On s.uuid = p.supplier_id
WHERE p.supplier_id = @supplierId
  And rd.created_on BETWEEN @dateFrom And @dateTo
GROUP BY
    supplier,
    p.description,
    buy_amt,
    sell_amt,
    rd.avail_quantity,
    rd.stock_quantity
ORDER BY rd.avail_quantity"

        Dim supplier = CType(cmbSupplier.SelectedItem, SupplierItem)

        Dim dateFrom = dtpFrom.SelectedDate.Value.Date
        Dim dateTo = dtpTo.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1)

        Dim items As New List(Of SupplierReportItem)
        Dim totalSales As Decimal = 0
        Dim counter As Integer = 1

        Using conn = PostgresConnection.GetConnection()
            conn.Open()

            Using cmd As New NpgsqlCommand(sql, conn)
                cmd.Parameters.Add("@supplierId", NpgsqlTypes.NpgsqlDbType.Uuid).
                Value = supplier.Uuid
                cmd.Parameters.Add("@dateFrom", NpgsqlTypes.NpgsqlDbType.Timestamp).
                Value = dateFrom
                cmd.Parameters.Add("@dateTo", NpgsqlTypes.NpgsqlDbType.Timestamp).
                Value = dateTo

                Using dr = cmd.ExecuteReader()
                    While dr.Read()

                        Dim qty = dr.GetInt32(2)
                        Dim sell = dr.GetDecimal(4)

                        totalSales += qty * sell

                        items.Add(New SupplierReportItem With {
                        .Index = counter,
                        .Supplier = dr.GetString(0),
                        .Product = dr.GetString(1),
                        .Quantity = qty,
                        .BuyPrice = If(currentUser.isAdmin, dr.GetDecimal(3), Nothing),
                        .SellPrice = sell,
                        .Barcode = dr.GetString(5),
                        .AvailableQty = dr.GetInt32(6),
                        .StockQty = dr.GetInt32(7)
                    })

                        counter += 1
                    End While
                End Using
            End Using
        End Using

        dgvReports.ItemsSource = items

        'If currentUser.isAdmin AndAlso chkBoxSalesPerSupplier.Checked Then
        'lblTotalSalesAmount.Text =
        '$"Συνολικό Ποσό Πωλήσεων: €{totalSales:N2}"
        'End If

        btnPrint.Visibility = Visibility.Visible

    End Sub



    Private Sub ClearGridAndSetInvisible()
        dgvReports.ItemsSource = Nothing
        dgvReports.Columns.Clear()
        dgvReports.Items.Clear()
    End Sub

    Private Sub ShowDateFields(show As Boolean)
        Dim v As Visibility = If(show, Visibility.Visible, Visibility.Collapsed)
        lblFromDate.Visibility = v
        lblToDate.Visibility = v
        dtpFrom.Visibility = v
        dtpTo.Visibility = v
    End Sub

    Private Sub FillCategories(addAll As Boolean)

        Dim sql As String =
        "SELECT uuid, description, vat
         FROM categories
         WHERE kioskid = @kioskid
         ORDER BY description ASC"

        Try
            Dim categories As New List(Of CategoryItem)

            If addAll Then
                categories.Add(New CategoryItem With {
                .Uuid = Guid.Empty,
                .Description = "Όλες",
                .Vat = 0D
            })
            End If

            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).
                    Value = Guid.Parse(kioskid)

                    Using dr = cmd.ExecuteReader()
                        While dr.Read()
                            categories.Add(New CategoryItem With {
                            .Uuid = dr.GetGuid(0),
                            .Description = dr.GetString(1),
                            .Vat = dr.GetDecimal(2)
                        })
                        End While
                    End Using
                End Using
            End Using

            cmbCategories.ItemsSource = categories

        Catch ex As Exception
            CreateExceptionFile($"FillCategories: {ex.Message}", sql)
            MessageBox.Show($"Error loading categories: {ex.Message}",
                        "Database Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error)
        End Try

    End Sub


    Private Sub FillUsers(addAll As Boolean)
        Dim WhoAmI As String = "FillUsers"
        Dim sql As String =
        "SELECT uuid, username
         FROM users
         WHERE kioskid = @kioskid
           AND deleted = FALSE
         ORDER BY username ASC"

        Try
            Dim users As New List(Of UserItem)

            If addAll Then
                users.Add(New UserItem With {
                .Uuid = Guid.Empty,
                .Username = "Όλοι"
            })
            End If

            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).
                    Value = Guid.Parse(kioskid)

                    Using dr = cmd.ExecuteReader()
                        While dr.Read()
                            users.Add(New UserItem With {
                            .Uuid = dr.GetGuid(0),
                            .Username = dr.GetString(1)
                        })
                        End While
                    End Using
                End Using
            End Using

            cmbUsers.ItemsSource = users

        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", sql)
            MessageBox.Show(
                $"Error loading categories: {ex.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            )
        End Try
    End Sub


    Private Sub SearchButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click
        Dim WhoAmI As String = "SearchButton_Click"
        Dim sql As String = ""
        dgvReports.Columns.Clear()
        dgvReports.Items.Clear()
        Try
            If rdbSalesPerCategory.IsChecked = True Then
                Dim dateFrom As DateTime =
        dtpFrom.SelectedDate.Value.Date

                Dim dateTo As DateTime =
        dtpTo.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1)

                Dim selectedCategory =
        TryCast(cmbCategories.SelectedItem, CategoryItem)

                Dim categoryName As String = "Όλες"
                Dim suppliers As String = "Όλοι"

                sql =
        "SELECT COALESCE(SUM(rd.amount),0)
         FROM receipts_det rd
         WHERE rd.created_on BETWEEN @dateFrom AND @dateTo"

                If selectedCategory IsNot Nothing AndAlso
       selectedCategory.Uuid <> Guid.Empty Then

                    sql &= "
            AND rd.product_serno IN (
                SELECT p.serno
                FROM products p
                WHERE p.category_id = @categoryId
            )"

                    categoryName = selectedCategory.Description
                End If

                Dim total As Decimal = 0D

                Using conn = PostgresConnection.GetConnection()
                    conn.Open()

                    Using cmd As New NpgsqlCommand(sql, conn)
                        cmd.Parameters.Add("@dateFrom",
                NpgsqlTypes.NpgsqlDbType.Timestamp).Value = dateFrom

                        cmd.Parameters.Add("@dateTo",
                NpgsqlTypes.NpgsqlDbType.Timestamp).Value = dateTo

                        If selectedCategory IsNot Nothing AndAlso
               selectedCategory.Uuid <> Guid.Empty Then
                            cmd.Parameters.Add("@categoryId",
                    NpgsqlTypes.NpgsqlDbType.Uuid).Value =
                        selectedCategory.Uuid
                        End If

                        total = CDec(cmd.ExecuteScalar())
                    End Using

                    ' 🔹 Suppliers
                    If selectedCategory IsNot Nothing AndAlso
           selectedCategory.Uuid <> Guid.Empty Then

                        Dim supSql As String =
                "SELECT DISTINCT s.s_name
                 FROM suppliers s
                 JOIN products p ON p.supplier_id = s.uuid
                 WHERE p.category_id = @categoryId"

                        Using cmd As New NpgsqlCommand(supSql, conn)
                            cmd.Parameters.Add("@categoryId",
                    NpgsqlTypes.NpgsqlDbType.Uuid).Value =
                        selectedCategory.Uuid

                            Using dr = cmd.ExecuteReader()
                                suppliers = ""
                                While dr.Read()
                                    suppliers &= dr.GetString(0) & " "
                                End While
                            End Using
                        End Using
                    End If
                End Using

                dgvReports.ItemsSource =
        New List(Of ReceiptReportItem) From {
            New ReceiptReportItem With {
                .dateFrom = dateFrom,
                .dateTo = dateTo,
                .TotalAmt = total,
                .Category = categoryName,
                .suppliers = suppliers.Trim()
            }
        }

                btnPrint.Visibility = Visibility.Visible

            ElseIf rdbSalesPerVAT.IsChecked = True Then
                Dim dateFrom = dtpFrom.SelectedDate
                Dim dateTo = dtpTo.SelectedDate

                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Από", .Binding = New Binding("From")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Έως", .Binding = New Binding("To")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ολικό Ποσό 0%", .Binding = New Binding("TotalAmt0Vat")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ολικό Ποσό 3%", .Binding = New Binding("TotalAmt3Vat")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ολικό Ποσό 5%", .Binding = New Binding("TotalAmt5Vat")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ολικό Ποσό 19%", .Binding = New Binding("TotalAmt19Vat")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Συνολικό Ποσό", .Binding = New Binding("TotalAmt")})

                'sql = "select NVL(sum(total_vat5),0), NVL(sum(total_vat19),0), NVL(sum(total_vat0),0), NVL(sum(total_vat3),0) from receipts " &
                '      "where created_on BETWEEN " &
                '      "to_timestamp('" & dateFrom & " 00:00:00', 'DD-MON-YY HH24:MI:SS') AND " &
                '      "to_timestamp('" & dateTo & " 23:59:59', 'DD-MON-YY HH24:MI:SS')"
                'cmd = New OracleCommand(sql, conn)

                'Dim totalVat0 As Double = 0
                'Dim totalVat5 As Double = 0
                'Dim totalVat19 As Double = 0
                'Dim totalVat3 As Double = 0
                'Using dr = cmd.ExecuteReader()
                '    If dr.Read() Then
                '        totalVat5 = CStr(CDbl(dr(0)).ToString("#,##0.00")) * (divideFactor5 / 100)
                '        totalVat19 = CStr(CDbl(dr(1)).ToString("#,##0.00")) * (divideFactor19 / 100)
                '        totalVat0 = CStr(CDbl(dr(2)).ToString("#,##0.00")) * (divideFactor0 / 100)
                '        totalVat3 = CStr(CDbl(dr(3)).ToString("#,##0.00")) * (divideFactor3 / 100)
                '    End If
                '    dr.Close()
                'End Using

                'Dim row As String() = New String() {dtpFrom.Text, dtpTo.Text, totalVat0.ToString("N2"), totalVat3.ToString("N2"), totalVat5.ToString("N2"), totalVat19.ToString("N2"), (totalVat0 + totalVat5 + totalVat19 + totalVat3).ToString("N2")}
                'dgvReports.Rows.Add(row)
                btnPrint.Visibility = Visibility.Visible

            ElseIf rdbXReport.IsChecked = True Then
                Dim dateFrom = dtpFrom.SelectedDate
                Dim dateTo = dtpTo.SelectedDate

                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Από", .Binding = New Binding("From")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Έως", .Binding = New Binding("To")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Χρήστης", .Binding = New Binding("User")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Αποδείξεις", .Binding = New Binding("Receipts")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ποσό 0%", .Binding = New Binding("totalAmt0Vat")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ποσό 3%", .Binding = New Binding("totalAmt3Vat")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ποσό 5%", .Binding = New Binding("totalAmt5Vat")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ποσό 19%", .Binding = New Binding("totalAmt19Vat")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ποσό Πωλήσεων", .Binding = New Binding("SalesAmt")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Αρχικό Ποσό", .Binding = New Binding("InitialAmt")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Πληρωμές Προμηθευτών", .Binding = New Binding("SupplierPayments")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ποσό VISA", .Binding = New Binding("AmountVisa")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Τελικό Ποσό Ταμείου για Παράδωση", .Binding = New Binding("TotalAmtToDeliver")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ποσό Λαχείων για Παράδωση", .Binding = New Binding("LotterAmt")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Αναλυτική Κατάσταση", .Binding = New Binding("DetailedStatement")})

                sql = "select from_date, to_date, u.username, total_receipts, total5percent, " &
                      "       total19percent, initial_amt, payments, final_amt, NVL(description,''), total0percent, " &
                      "       amount_laxeia, initialAmtLaxeia, amountvisa, finalamtlaxeia, total3percent " &
                      "from x_report x " &
                      "inner join users u on x.user_id = u.uuid " &
                      "where (total_receipts > 0 or payments > 0) and created_on BETWEEN " &
                      "to_timestamp('" & dateFrom & " 00:00:00', 'DD-MON-YY HH24:MI:SS') AND " &
                      "to_timestamp('" & dateTo & " 23:59:59', 'DD-MON-YY HH24:MI:SS') "

                'If cmbUsers.SelectedIndex <> -1 Then
                'If Not cmbUsers.SelectedItem.Equals("Όλοι") Then
                'sql += " and user_id = '" & userUUIDs(cmbUsers.SelectedIndex) & "' "
                'End If
                'End If

                sql += " order by from_date, to_date"

                'cmd = New OracleCommand(sql, conn)
                'Using dr = cmd.ExecuteReader()
                '    While dr.Read()
                '        Dim total0percent As Double = CDbl(dr(10)) * (divideFactor0 / 100)
                '        Dim total5percent As Double = CDbl(dr(4)) * (divideFactor5 / 100)
                '        Dim total19percent As Double = CDbl(dr(5)) * (divideFactor19 / 100)
                '        Dim initial_amt As Double = CDbl(dr(6))
                '        Dim payments As Double = CDbl(dr(7))
                '        Dim total3percent As Double = 0
                '        If Not IsDBNull(dr(15)) Then
                '            total3percent = CDbl(dr(15)) * (divideFactor3 / 100)
                '        End If
                '        Dim final_amt As Double = (total0percent + total3percent + total5percent + total19percent)
                '        Dim amountLaxeia As Double = CDbl(dr(11))

                '        Dim initialAmountLaxeia As Double = 0
                '        If Not IsDBNull(dr(12)) Then
                '            initialAmountLaxeia = CDbl(dr(12))
                '        End If

                '        Dim visaAmount As Double = 0
                '        If Not IsDBNull(dr(13)) Then
                '            visaAmount = CDbl(dr(13)) '* (divideFactor / 100)
                '        End If

                '        Dim salesDescription = ""
                '        If Not dr.IsDBNull(9) Then
                '            salesDescription = CStr(dr(9))
                '        End If
                '        Dim finalAmtLaxeia As Double = 0
                '        If Not IsDBNull(dr(14)) Then
                '            finalAmtLaxeia = CDbl(dr(14))
                '        End If
                '        Dim totalAmountDeliver As Double = (total0percent + total3percent + total5percent + total19percent + initial_amt) - payments - visaAmount

                '        Dim row As String() = New String() {CStr(dr(0)), CStr(dr(1)), CStr(dr(2)), CInt(dr(3)), total0percent.ToString("N2"), total3percent.ToString("N2"), total5percent.ToString("N2"), total19percent.ToString("N2"), final_amt.ToString("N2"), initial_amt.ToString("N2"), payments.ToString("N2"), visaAmount.ToString("N2"), totalAmountDeliver.ToString("N2"), finalAmtLaxeia.ToString("N2")}
                '        dgvReports.Rows.Add(row)
                '    End While
                'End Using
                btnPrint.Visibility = Visibility.Visible

            ElseIf rdbZReport.IsChecked = True Then
                ClearGridAndSetInvisible()

                If Not dtpFrom.SelectedDate.HasValue OrElse Not dtpTo.SelectedDate.HasValue Then
                    MessageBox.Show("Παρακαλώ επιλέξτε και τις δύο ημερομηνίες", "Σφάλμα", MessageBoxButton.OK, MessageBoxImage.Error)
                    Exit Sub
                End If

                Dim fromDate As Date = dtpFrom.SelectedDate.Value
                Dim toDate As Date = dtpTo.SelectedDate.Value

                ' --- Validate date order
                If toDate < fromDate Then
                    MessageBox.Show("Η ημερομηνία Έως δεν μπορεί να είναι μικρότερη από την ημερομηνία Από", "Σφάλμα", MessageBoxButton.OK, MessageBoxImage.Error)
                    Exit Sub
                End If

                ' --- Validate against start date
                If fromDate < startDate Then
                    MessageBox.Show("Η ημερομηνία Από δεν μπορεί να είναι μικρότερη από την αρχική ημερομηνία", "Σφάλμα", MessageBoxButton.OK, MessageBoxImage.Error)
                    Exit Sub
                End If

                If toDate < startDate Then
                    MessageBox.Show("Η ημερομηνία Έως δεν μπορεί να είναι μικρότερη από την αρχική ημερομηνία", "Σφάλμα", MessageBoxButton.OK, MessageBoxImage.Error)
                    Exit Sub
                End If

                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Z", .Binding = New Binding("Z")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Άπό", .Binding = New Binding("From")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Έως", .Binding = New Binding("To")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Αποδείξεις", .Binding = New Binding("Receipts")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ολικό Ποσό 0%", .Binding = New Binding("TotalAmt0Vat")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ολικό Ποσό 3%", .Binding = New Binding("TotalAmt3Vat")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ολικό Ποσό 5%", .Binding = New Binding("TotalAmt5Vat")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ολικό Ποσό 19%", .Binding = New Binding("TotalAmt19Vat")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Συνολικό Ποσό", .Binding = New Binding("TotalAmt")})


                'Dim tmpFrom As Date = dtpFrom.Value.AddHours(-dtpFrom.Value.Hour)
                'tmpFrom = tmpFrom.AddMinutes(-tmpFrom.Minute)
                'tmpFrom = tmpFrom.AddSeconds(-tmpFrom.Second)

                'Dim tmpTo As Date = dtpTo.Value.AddHours(-dtpTo.Value.Hour)
                'tmpTo = tmpTo.AddMinutes(-tmpTo.Minute)
                'tmpTo = tmpTo.AddSeconds(-tmpTo.Second)
                'Dim dateFrom As String = CStr(tmpFrom.Day) & "-" & findMonth(CStr(tmpFrom.Month)) & "-" & CStr(tmpFrom.Year).Substring(2, 2)


                ' Normalize start and end dates
                Dim tmpFrom As Date = dtpFrom.SelectedDate.Value.Date
                Dim tmpTo As Date = dtpTo.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1)

                ' Use findMonth ONLY for display purposes
                Dim dateFrom As String = CStr(tmpFrom.Day) & "-" & FindMonth(CStr(tmpFrom.Month)) & "-" & CStr(tmpFrom.Year).Substring(2, 2)
                Dim dateTo As String = CStr(tmpTo.Day) & "-" & FindMonth(CStr(tmpTo.Month)) & "-" & CStr(tmpTo.Year).Substring(2, 2)

                ' For SQL, use tmpFrom/tmpTo as Date parameters
                'cmd.Parameters.AddWithValue("@from", tmpFrom)
                'cmd.Parameters.AddWithValue("@to", tmpTo)


                'While (1)
                '    If tmpFrom > tmpTo Then
                '        Exit Sub
                '    End If


                '    Dim totalReceipts As Integer = 0
                '    Dim totalVat0 As Double = 0
                '    Dim totalVat5 As Double = 0
                '    Dim totalVat19 As Double = 0
                '    Dim totalAll As Double = 0
                '    Dim totalVat3 As Double = 0
                '    Dim zseq As Integer = -1
                '    Dim zDate As String

                '    Dim tmpDate As String = CStr(tmpFrom.Day) & "-" & findMonth(CStr(tmpFrom.Month)) & "-" & CStr(tmpFrom.Year)
                '    sql = "select z_seq, z_date, total_receipts, total_amount0, total_amount5, total_amount19, total_amount, nvl(total_amount3,0) from z_report " &
                '          "where z_date='" & tmpDate & "'"
                '    cmd = New OracleCommand(sql, conn)
                '    Using dr = cmd.ExecuteReader()
                '        If dr.Read Then
                '            zseq = CInt(dr(0))
                '            zDate = dr(1)
                '            totalReceipts = CInt(dr(2))
                '            totalVat0 = CStr(CDbl(dr(3)).ToString("#,##0.00"))
                '            totalVat5 = CStr(CDbl(dr(4)).ToString("#,##0.00"))
                '            totalVat19 = CStr(CDbl(dr(5)).ToString("#,##0.00"))
                '            totalAll = CStr(CDbl(dr(6)).ToString("#,##0.00"))
                '            totalVat3 = CStr(CDbl(dr(7)).ToString("#,##0.00"))

                '            Dim row As String() = New String() {zseq, zDate, zDate, totalReceipts, totalVat0.ToString("N2"), totalVat3.ToString("N2"), totalVat5.ToString("N2"),
                '                    totalVat19.ToString("N2"), totalAll.ToString("N2")}
                '            dgvReports.Rows.Add(row)

                '        Else
                '            sql = "select NVL(sum(total_vat5),0), NVL(sum(total_vat19),0), NVL(sum(total_vat0),0), NVL(sum(total_vat3),0), count(*) from receipts " &
                '              "where created_on BETWEEN " &
                '              "to_timestamp('" & dateFrom & " 00:00:00', 'DD-MON-YY HH24:MI:SS') AND " &
                '              "to_timestamp('" & dateFrom & " 23:59:59', 'DD-MON-YY HH24:MI:SS')"

                '            cmd = New OracleCommand(sql, conn)
                '            Using drInner = cmd.ExecuteReader()
                '                If drInner.Read() Then
                '                    totalVat5 = CStr(CDbl(drInner(0)).ToString("#,##0.00")) * (divideFactor5 / 100)
                '                    totalVat19 = CStr(CDbl(drInner(1)).ToString("#,##0.00")) * (divideFactor19 / 100)
                '                    totalVat0 = CStr(CDbl(drInner(2)).ToString("#,##0.00")) * (divideFactor0 / 100)
                '                    totalVat3 = CStr(CDbl(drInner(3)).ToString("#,##0.00")) * (divideFactor3 / 100)
                '                    totalReceipts = CInt(drInner(4))
                '                End If
                '            End Using
                '            zseq = getZseq(tmpFrom)

                '            If zseq = -1 Then
                '                MessageBox.Show("Δεν έχετε εκτυπώσει όλα τα Z-Report των προηγούμενων ημερομηνιών", "Σφάλμα", MessageBoxButtons.OK, MessageBoxIcon.Error)
                '                Exit Sub
                '            End If

                '            Dim row As String() = New String() {zseq, tmpFrom.Day & "-" & tmpFrom.Month & "-" & tmpFrom.Year,
                '                                            tmpFrom.Day & "-" & tmpFrom.Month & "-" & tmpFrom.Year,
                '                                            totalReceipts, totalVat0.ToString("N2"), totalVat5.ToString("N2"),
                '                                            totalVat19.ToString("N2"), totalVat3.ToString("N2"), (totalVat0 + totalVat3 + totalVat5 + totalVat19).ToString("N2")}
                '            dgvReports.Rows.Add(row)

                '            sql = "update z_report set total_receipts = " & totalReceipts & ", " &
                '                  "                    total_amount0 = " & totalVat0 & ", " &
                '                  "                    total_amount3 = " & totalVat3 & ", " &
                '                  "                    total_amount5 = " & totalVat5 & ", " &
                '                  "                    total_amount19 = " & totalVat19 & ", " &
                '                  "                    total_amount = " & (totalVat0 + totalVat3 + totalVat5 + totalVat19) & " " &
                '                  "where z_seq = " & zseq & ""
                '            cmd = New OracleCommand(sql, conn)
                '            cmd.ExecuteNonQuery()
                '        End If

                '        btnPrint.Visible = True
                '    End Using
                '    tmpFrom = tmpFrom.AddDays(1)
                '    dateFrom = CStr(tmpFrom.Day) & "-" & findMonth(CStr(tmpFrom.Month)) & "-" & CStr(tmpFrom.Year).Substring(2, 2)
                'End While

            ElseIf rdbUsers.IsChecked = True Then
                ClearGridAndSetInvisible()

                If cmbUsers.SelectedItem Is Nothing Then
                    MessageBox.Show("Δεν έχετε επιλέξει χρήστη",
                                    "Επιλογή Χρήστη",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error)
                    Exit Sub
                End If

                Dim selectedUser = CType(cmbUsers.SelectedItem, UserItem)

                Dim dateFrom As DateTime = dtpFrom.SelectedDate.Value.Date
                Dim dateTo As DateTime = dtpTo.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1)
                Dim totalHours As Double = 0
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Από", .Binding = New Binding("From")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Έως", .Binding = New Binding("To")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Διάρκεια (Λεπτά)", .Binding = New Binding("DurationMinutes")})

                sql =
                    "SELECT login_when, logout_when
                     FROM sessions
                     WHERE kioskid = @kioskid AND user_id = @userId
                     AND login_when BETWEEN @dateFrom AND @dateTo
                     ORDER BY login_when ASC"

                Using conn = PostgresConnection.GetConnection()
                    conn.Open()

                    Using cmd As New NpgsqlCommand(sql, conn)
                        cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)
                        cmd.Parameters.Add("@userId", NpgsqlTypes.NpgsqlDbType.Uuid).Value = selectedUser.Uuid
                        cmd.Parameters.Add("@dateFrom", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = dateFrom
                        cmd.Parameters.Add("@dateTo", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = dateTo

                        Using dr = cmd.ExecuteReader()
                            While dr.Read()

                                Dim loginWhen = dr.GetDateTime(0)
                                Dim logoutWhen = If(dr.IsDBNull(1),
                                                     DateTime.Now,
                                                     dr.GetDateTime(1))

                                Dim minutes As Integer =
                                    CInt((logoutWhen - loginWhen).TotalMinutes)

                                totalHours += minutes / 60.0
                                dgvReports.Items.Add(New With {
                                    .From = loginWhen,
                                    .To = logoutWhen,
                                    .DurationMinutes = minutes
                                    })
                            End While
                        End Using
                    End Using
                End Using
                txtBoxTotalHoursOrPayments.Text = totalHours.ToString("N2")
                btnPrint.Visibility = Visibility.Visible
            ElseIf rdbPayments.IsChecked = True Then

                Dim dateFrom = dtpFrom.SelectedDate
                Dim dateTo = dtpTo.SelectedDate

                '--- Columns
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Από", .Binding = New Binding("From")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Έως", .Binding = New Binding("To")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ημερομηνία Πληρωμής", .Binding = New Binding("PaymentDate")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ποσό", .Binding = New Binding("Amount")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Χρήστης", .Binding = New Binding("User")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Φ.Π.Α", .Binding = New Binding("VAT")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ποσό Φ.Π.Α", .Binding = New Binding("VatAmount")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Προμηθευτής", .Binding = New Binding("Supplier")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Αρ. Τιμολογίου", .Binding = New Binding("InvNumber")})

                sql = "
                    SELECT 
                        p.created_on,
                        p.amount,
                        u.username,
                        COALESCE(p.vat::text, 'N/A') AS vat_label,
                        COALESCE(p.amountvat, 0) AS amountvat,
                        COALESCE(p.vat::text, '-1') AS vat_value,
                        COALESCE(s.s_name::text, ' ') AS s_name,
                        COALESCE(p.inv_number::text, ' ') AS inv_number
                    FROM payments p
                    INNER JOIN users u ON p.created_by = u.uuid
                    LEFT JOIN suppliers s ON p.supplier_id = s.uuid
                    WHERE p.kioskid = @kioskid
                      AND p.created_on BETWEEN @from AND @to
                    ORDER BY p.created_on DESC;
                   "

                            Dim totalAmount As Double = 0
                            Dim totalVATamount As Double = 0

                            Dim totalVat0 As Double = 0
                            Dim totalVat3 As Double = 0
                            Dim totalVat5 As Double = 0
                            Dim totalVat19 As Double = 0
                            Dim totalPaymentsVat0 As Double = 0
                            Dim totalPaymentsVat3 As Double = 0
                            Dim totalPaymentsVat5 As Double = 0
                            Dim totalPaymentsVat19 As Double = 0
                            Using conn = PostgresConnection.GetConnection()
                                conn.Open()
                                Using cmd As New NpgsqlCommand(sql, conn)
                                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)
                                    cmd.Parameters.Add("@from", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = dateFrom.Value.Date
                                    cmd.Parameters.Add("@to", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = dateTo.Value.Date.AddDays(1).AddSeconds(-1)

                                    Using dr = cmd.ExecuteReader()
                                        While dr.Read()
                                            Dim amount As Double = CDbl(dr("amount"))
                                            totalAmount += amount
                                            Dim vatAmount As Double = CDbl(dr("amountvat"))
                                            totalVATamount += vatAmount

                                            Dim vatValue As Integer = Convert.ToInt32(dr("vat_value"))

                                            Select Case vatValue
                                                Case 0
                                                    totalVat0 += vatAmount
                                                    totalPaymentsVat0 += amount
                                                Case 3
                                                    totalVat3 += vatAmount
                                                    totalPaymentsVat3 += amount
                                                Case 5
                                                    totalVat5 += vatAmount
                                                    totalPaymentsVat5 += amount
                                                Case 19
                                                    totalVat19 += vatAmount
                                                    totalPaymentsVat19 += amount
                                            End Select

                                            dgvReports.Items.Add(New With {
                    .From = dateFrom.Value.ToString("dd-MM-yyyy"),
                    .To = dateTo.Value.ToString("dd-MM-yyyy"),
                    .PaymentDate = Convert.ToDateTime(dr("created_on")).ToString("dd-MM-yyyy HH:mm:ss"),
                    .Amount = amount.ToString("N2"),
                    .User = dr("username").ToString(),
                    .VAT = dr("vat_label").ToString(),
                    .VatAmount = vatAmount.ToString("N2"),
                    .Supplier = dr("s_name").ToString(),
                    .InvNumber = dr("inv_number").ToString()
                })
                                        End While
                                    End Using
                                End Using
                            End Using

                            txtBoxTotalHoursOrPayments.Text = "€" & TruncateDecimal(totalAmount + totalVATamount, 3).ToString
                            lblAmountVAT.Content = "Φ.Π.Α για Επιστροφή: €" + TruncateDecimal(totalVATamount, 3).ToString + vbNewLine +
                "Πληρωμές (με Φ.Π.Α) 0% : " + TruncateDecimal(totalPaymentsVat0 + totalVat0, 3).ToString + " , Φ.Π.Α. 0%: " + TruncateDecimal(totalVat0, 3).ToString + vbNewLine +
                "Πληρωμές (με Φ.Π.Α) 3% : " + TruncateDecimal(totalPaymentsVat3 + totalVat3, 3).ToString + " , Φ.Π.Α. 3%: " + TruncateDecimal(totalVat3, 3).ToString + vbNewLine +
                "Πληρωμές (με Φ.Π.Α) 5% : " + TruncateDecimal(totalPaymentsVat5 + totalVat5, 3).ToString + " , Φ.Π.Α. 5%: " + TruncateDecimal(totalVat5, 3).ToString + vbNewLine +
                "Πληρωμές (με Φ.Π.Α) 19%: " + TruncateDecimal(totalPaymentsVat19 + totalVat19, 3).ToString + ", Φ.Π.Α. 19%: " + TruncateDecimal(totalVat19, 3).ToString
                            btnPrint.Visibility = Visibility.Visible

                        ElseIf rdbQntHistory.IsChecked = True Then
                            Dim dateFrom = dtpFrom.SelectedDate
                            Dim dateTo = dtpTo.SelectedDate
                            dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Barcode", .Binding = New Binding("Barcode")})
                            dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Προϊόν", .Binding = New Binding("Product")})
                            dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Προηγούμενη Ποσότητα", .Binding = New Binding("PreviousQuantity")})
                            dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Νέα Ποσότητα", .Binding = New Binding("NewQuantity")})
                            dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Προηγ.Ποσ. Αποθήκης", .Binding = New Binding("PreviousStockQuantity")})
                            dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Νέα Ποσ. Αποθήκης", .Binding = New Binding("NewStockQuantity")})
                            dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Ημερομηνία Αλλαγής", .Binding = New Binding("DateOfChange")})
                            dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Χρήστης Αλλαγής", .Binding = New Binding("ChangeUser")})
                            dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Προηγ. Τιμή", .Binding = New Binding("PreviousPrice")})
                            dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "", .Binding = New Binding("Νέα Τιμή")})

                            'sql = "select (select barcode from BARCODES where product_serno = pa.PRODUCT_SERNO and rownum < 2) barcode, " &
                            '      "p.DESCRIPTION, pa.PREV_QUANTITY, pa.NEW_QUANTITY, nvl(pa.PREV_ST_QNT,0), nvl(pa.NEW_ST_QNT,0), pa.MODIFIED_WHEN, " &
                            '      "u.USERNAME, pa.OLD_PRICE, pa.NEW_PRICE " &
                            '      "from products_audit pa " &
                            '      "inner join products p on pa.PRODUCT_SERNO = p.serno " &
                            '      "inner join users u on u.UUID = pa.MODIFIED_BY " &
                            '      "where modified_when BETWEEN " &
                            '      "to_timestamp('" & dateFrom & " 00:00:00', 'DD-MON-YY HH24:MI:SS') AND " &
                            '      "to_timestamp('" & dateTo & " 23:59:59', 'DD-MON-YY HH24:MI:SS') order by pa.MODIFIED_WHEN desc"
                            'cmd = New OracleCommand(sql, conn)

                            'Using dr = cmd.ExecuteReader()
                            '    While dr.Read()
                            '        Dim row As String() = New String() {dr(0), dr(1), dr(2), dr(3), dr(4), dr(5), dr(6), dr(7), dr(8), dr(9)}
                            '        dgvReports.Rows.Add(row)
                            '    End While
                            'End Using

                            btnPrint.Visibility = Visibility.Visible

                        ElseIf rdbSessions.IsChecked = True Then
                            Dim dateFrom = dtpFrom.SelectedDate
                Dim dateTo = dtpTo.SelectedDate
                '--- Columns
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Χρήστης", .Binding = New Binding("Username")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Από", .Binding = New Binding("LoginWhen")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Έως", .Binding = New Binding("LogoutWhen")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Μηχανή", .Binding = New Binding("Machine")})

                If Not dateFrom.HasValue OrElse Not dateTo.HasValue Then
                    MessageBox.Show("Please select both dates.")
                    Exit Sub
                End If

                sql = "SELECT u.username, login_when, logout_when, machine_name " &
                        "FROM sessions s " &
                        "JOIN users u ON s.user_id = u.uuid " &
                        "WHERE u.kioskid = @kioskid AND login_when BETWEEN @from AND @to " &
                        "ORDER BY login_when;"
                Using conn = PostgresConnection.GetConnection()
                    conn.Open()
                    Using cmd As New NpgsqlCommand(sql, conn)
                        cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)
                        cmd.Parameters.AddWithValue("@from", dateFrom.Value.Date)
                        cmd.Parameters.AddWithValue("@to", dateTo.Value.Date.AddDays(1).AddSeconds(-1))

                        Using dr = cmd.ExecuteReader()
                            While dr.Read()
                                dgvReports.Items.Add(New With {
                            .Username = dr.GetString(0),
                            .LoginWhen = dr.GetDateTime(1),
                            .LogoutWhen = If(dr.IsDBNull(2), "", dr.GetDateTime(2).ToString()),
                            .Machine = dr.GetString(3)
                        })
                            End While
                        End Using
                    End Using
                End Using
                btnPrint.Visibility = Visibility.Collapsed
            End If
            FormatDataGrid()
        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", sql)
            MessageBox.Show(
                $"Error loading categories: {ex.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            )
        End Try
    End Sub

    Private Sub FormatDataGrid()
        If dgvReports.Items.Count > 0 Then
            dgvReports.ScrollIntoView(dgvReports.Items(dgvReports.Items.Count - 1))
        End If
    End Sub

End Class
