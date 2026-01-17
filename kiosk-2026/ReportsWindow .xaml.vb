Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Data
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text.Encodings
Imports System.Windows.Interop
Imports kiosk_2026.CategoriesWindow
Imports Npgsql

Public Class ReportsWindow
    Inherits Window


    Private Const GWL_STYLE As Integer = -16
    Private Const WS_SYSMENU As Integer = &H80000

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
        'chkBoxSalesPerSupplier.IsChecked = False

        'cmbCategories.Visibility = Visibility.Collapsed

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
            'chkBoxSalesPerSupplier.Visibility = Visibility.Visible
            'cmbSupplier.Visibility = Visibility.Visible
            'fillSuppliers()
        Else
            'chkBoxSalesPerSupplier.Visibility = Visibility.Collapsed
            'cmbSupplier.Visibility = Visibility.Collapsed
        End If

        ' ---------- PAYMENTS VAT ----------
        If reportType = "PAYMENTS" AndAlso currentUser.isAdmin Then
            'lblAmountVAT.Visibility = Visibility.Visible
        Else
            'lblAmountVAT.Visibility = Visibility.Collapsed
            'lblAmountVAT.Content = ""
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
            showDateFields(True)
        Else
            showDateFields(False)
        End If

        ' ---------- QUANTITY / BUY SELL ----------
        If reportType = "QUANTITY_PER_PRODUCT" OrElse reportType = "BUY_SELL" Then
            btnPrint.Visibility = Visibility.Collapsed
            'cmbUsers.Visibility = Visibility.Collapsed
            'lblTotalHoursOrAmount.Visibility = Visibility.Collapsed
            'txtBoxTotalHoursOrPayments.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed            
            'txtBoxBarcode.Focus()
        End If
        ' ---------- SALES PER PRODUCT ----------
        If reportType = "SALES_PER_PRODUCT" Then
            'fillProductsNoBarcode()
            btnPrint.Visibility = Visibility.Collapsed
            'cmbUsers.Visibility = Visibility.Collapsed
            'lblTotalHoursOrAmount.Visibility = Visibility.Collapsed
            'txtBoxTotalHoursOrPayments.Visibility = Visibility.Collapsed
            'lblAmountVAT.Visibility = Visibility.Visible
            'cmbNoBarcode.Visibility = Visibility.Visible
            'txtBoxBarcode.Focus()

            ' ---------- PAYMENTS ----------
        ElseIf reportType = "PAYMENTS" Then
            btnPrint.Visibility = Visibility.Collapsed
            'cmbUsers.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed

            'lblTotalHoursOrAmount.Visibility = Visibility.Visible
            'txtBoxTotalHoursOrPayments.Visibility = Visibility.Visible
            'txtBoxTotalHoursOrPayments.Text = "0"
            'lblTotalHoursOrAmount.Content = "Σύνολο"

            ' ---------- VAT / Z REPORT ----------
        ElseIf reportType = "SALES_PER_VAT" OrElse reportType = "Z_REPORT" Then
            btnPrint.Visibility = Visibility.Collapsed
            'cmbUsers.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed

            ' ---------- X REPORT ----------
        ElseIf reportType = "X_REPORT" Then
            btnPrint.Visibility = Visibility.Collapsed
            'lblTotalHoursOrAmount.Visibility = Visibility.Collapsed
            'txtBoxTotalHoursOrPayments.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed

            'cmbUsers.Visibility = Visibility.Visible
            'fillUsers(1)

            ' ---------- PRODUCTS PER SUPPLIER ----------
        ElseIf reportType = "PRODUCTS_PER_SUPPLIER" Then
            btnPrint.Visibility = Visibility.Collapsed
            'cmbUsers.Visibility = Visibility.Collapsed
            'lblTotalHoursOrAmount.Visibility = Visibility.Collapsed
            'txtBoxTotalHoursOrPayments.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed

            ' ---------- HOURS PER USER ----------
        ElseIf reportType = "HOURS_PER_USER" Then
            'cmbUsers.Visibility = Visibility.Visible
            'fillUsers(-1)

            'lblTotalHoursOrAmount.Visibility = Visibility.Visible
            'txtBoxTotalHoursOrPayments.Visibility = Visibility.Visible
            'txtBoxTotalHoursOrPayments.Text = "0"
            'lblTotalHoursOrAmount.Content = "Σύνολο Ωρών"
            btnPrint.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed

            ' ---------- LOGIN HISTORY ----------
        ElseIf reportType = "LOGIN_HISTORY" Then
            btnPrint.Visibility = Visibility.Collapsed
            'lblTotalHoursOrAmount.Visibility = Visibility.Collapsed
            'txtBoxTotalHoursOrPayments.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed
            'cmbUsers.Visibility = Visibility.Collapsed

            ' ---------- SALES PER CATEGORY ----------
        ElseIf reportType = "SALES_PER_CATEGORY" Then
            btnPrint.Visibility = Visibility.Collapsed
            'lblTotalHoursOrAmount.Visibility = Visibility.Collapsed
            'txtBoxTotalHoursOrPayments.Visibility = Visibility.Collapsed
            'cmbNoBarcode.Visibility = Visibility.Collapsed
            'cmbUsers.Visibility = Visibility.Collapsed

            'cmbCategories.Visibility = Visibility.Visible
            'FillCategories(1)
        End If

    End Sub

    Private Sub ClearGridAndSetInvisible()
        dgvReports.ItemsSource = Nothing
        dgvReports.Columns.Clear()
        'dgvReports.Items.Clear()
        'dgvReports.Columns.Clear()
    End Sub

    Private Sub ShowDateFields(show As Boolean)
        Dim v As Visibility = If(show, Visibility.Visible, Visibility.Collapsed)
        lblFromDate.Visibility = v
        lblToDate.Visibility = v
        dtpFrom.Visibility = v
        dtpTo.Visibility = v
    End Sub


    Private Sub fillcategories(ByVal addall As Integer)

        'Try
        'Using cmd As New Npgsql.NpgsqlCommand(q_get_categories, conn)
        'cmd.CommandType = CommandType.Text

        'Using dr As Npgsql.NpgsqlDataReader = cmd.ExecuteReader()

        'categoryid = ""
        'categoryuuids.clear()
        'cmbcategories.items.clear()

        'If addall = 1 Then
        'categoryuuids.add(-1)
        'cmbcategories.items.add("όλες")
        'End If

        'While dr.Read()
        'categoryuuids.add(dr("uuid"))
        'cmbcategories.items.add(dr("description"))
        'End While

        'End Using
        'End Using

        'Catch ex As Exception
        'CreateExceptionFile(ex.Message, " " & q_get_categories)

        'MessageBox.Show(ex.Message, application_error,
        'MessageBoxButton.OK, MessageBoxImage.Error)
        'End Try

    End Sub


    Private Sub SearchButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click
        Dim WhoAmI As String = "SearchButton_Click"
        Dim sql As String = ""

        Try
            If rdbSalesPerCategory.IsChecked = True Then
                'dgvReports.Columns.Clear()
                'dgvReports.Rows.Clear()

                'Dim dateFrom As String = CStr(dtpFrom.Value.Day) & "-" & findMonth(CStr(dtpFrom.Value.Month)) & "-" & CStr(dtpFrom.Value.Year).Substring(2, 2)
                'Dim dateTo As String = CStr(dtpTo.Value.Day) & "-" & findMonth(CStr(dtpTo.Value.Month)) & "-" & CStr(dtpTo.Value.Year).Substring(2, 2)

                'sql = "select NVL(sum(amount),0) total from receipts_det " &
                '      "where created_on BETWEEN " &
                '      "to_timestamp('" & dateFrom & " 00:00:00', 'DD-MON-YY HH24:MI:SS') AND " &
                '      "to_timestamp('" & dateTo & " 23:59:59', 'DD-MON-YY HH24:MI:SS')"

                'Dim categoryName = "Όλες"
                'Dim supplierName = "Όλοι"
                'If cmbCategories.SelectedIndex <> -1 Then
                '    If Not cmbCategories.SelectedItem.Equals("Όλες") Then
                '        sql += " and product_serno in (select serno from products where CATEGORY_ID = '" & categoryUUIDs(cmbCategories.SelectedIndex) & "') "
                '        categoryName = cmbCategories.SelectedItem
                '    End If
                'End If

                'cmd = New OracleCommand(sql, conn)
                'Dim total As Double = 0
                'Using dr = cmd.ExecuteReader()
                '    If dr.Read() Then
                '        total = CStr(CDbl(dr(0)).ToString("#,##0.00"))
                '    End If
                'End Using

                'dgvReports.ColumnCount = 5

                'dgvReports.Columns(0).Name = FROM_DATE
                'dgvReports.Columns(0).Width = 150

                'dgvReports.Columns(1).Name = "Έως"
                'dgvReports.Columns(1).Width = 150

                'dgvReports.Columns(2).Name = "Ολικό Ποσό"
                'dgvReports.Columns(2).Width = 100

                'dgvReports.Columns(3).Name = "Κατηγορία"
                'dgvReports.Columns(3).Width = 100

                'dgvReports.Columns(4).Name = "Προμηθευτές"
                'dgvReports.Columns(4).Width = 400

                ''Get suppliers
                'If Not categoryName.Equals("Ολες") And cmbCategories.SelectedIndex > 0 Then
                '    sql = "select s_name from suppliers " &
                '          "where uuid in (select supplier_id from products where serno in (" &
                '                            "select serno from products where CATEGORY_ID = '" & categoryUUIDs(cmbCategories.SelectedIndex) & "'))"

                '    supplierName = ""
                '    cmd = New OracleCommand(sql, conn)
                '    Using dr = cmd.ExecuteReader()
                '        While dr.Read()
                '            supplierName += " " + CStr(dr(0))
                '        End While
                '    End Using
                'End If

                'Dim row As String() = New String() {dtpFrom.Text, dtpTo.Text, total.ToString("N2"), categoryName, supplierName}
                'dgvReports.Rows.Add(row)
                btnPrint.Visibility = Visibility.Visible

            ElseIf rdbSalesPerVAT.IsChecked = True Then
                'dgvReports.Columns.Clear()
                'dgvReports.Rows.Clear()

                'Dim dateFrom As String = CStr(dtpFrom.Value.Day) & "-" & findMonth(CStr(dtpFrom.Value.Month)) & "-" & CStr(dtpFrom.Value.Year).Substring(2, 2)
                'Dim dateTo As String = CStr(dtpTo.Value.Day) & "-" & findMonth(CStr(dtpTo.Value.Month)) & "-" & CStr(dtpTo.Value.Year).Substring(2, 2)

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

                'dgvReports.ColumnCount = 7

                'dgvReports.Columns(0).Name = FROM_DATE
                'dgvReports.Columns(0).Width = 250

                'dgvReports.Columns(1).Name = "Έως"
                'dgvReports.Columns(1).Width = 250

                'dgvReports.Columns(2).Name = "Ολικό Ποσό 0%"
                'dgvReports.Columns(2).Width = 100

                'dgvReports.Columns(3).Name = "Ολικό Ποσό 3%"
                'dgvReports.Columns(3).Width = 100

                'dgvReports.Columns(4).Name = "Ολικό Ποσό 5%"
                'dgvReports.Columns(4).Width = 100

                'dgvReports.Columns(5).Name = "Ολικό Ποσό 19%"
                'dgvReports.Columns(5).Width = 100

                'dgvReports.Columns(6).Name = "Συνολικό Ποσό"
                'dgvReports.Columns(6).Width = 149

                'Dim row As String() = New String() {dtpFrom.Text, dtpTo.Text, totalVat0.ToString("N2"), totalVat3.ToString("N2"), totalVat5.ToString("N2"), totalVat19.ToString("N2"), (totalVat0 + totalVat5 + totalVat19 + totalVat3).ToString("N2")}
                'dgvReports.Rows.Add(row)
                btnPrint.Visibility = Visibility.Visible

            ElseIf rdbXReport.IsChecked = True Then
                'dgvReports.Columns.Clear()
                'dgvReports.Rows.Clear()
                'Dim dateFrom As String = CStr(dtpFrom.Value.Day) & "-" & findMonth(CStr(dtpFrom.Value.Month)) & "-" & CStr(dtpFrom.Value.Year).Substring(2, 2)
                'Dim dateTo As String = CStr(dtpTo.Value.Day) & "-" & findMonth(CStr(dtpTo.Value.Month)) & "-" & CStr(dtpTo.Value.Year).Substring(2, 2)

                'sql = "select from_date, to_date, u.username, total_receipts, total5percent, " &
                '      "       total19percent, initial_amt, payments, final_amt, NVL(description,''), total0percent, " &
                '      "       amount_laxeia, initialAmtLaxeia, amountvisa, finalamtlaxeia, total3percent " &
                '      "from x_report x " &
                '      "inner join users u on x.user_id = u.uuid " &
                '      "where (total_receipts > 0 or payments > 0) and created_on BETWEEN " &
                '      "to_timestamp('" & dateFrom & " 00:00:00', 'DD-MON-YY HH24:MI:SS') AND " &
                '      "to_timestamp('" & dateTo & " 23:59:59', 'DD-MON-YY HH24:MI:SS') "

                'If cmbUsers.SelectedIndex <> -1 Then
                '    If Not cmbUsers.SelectedItem.Equals("Όλοι") Then
                '        sql += " and user_id = '" & userUUIDs(cmbUsers.SelectedIndex) & "' "
                '    End If
                'End If

                'sql += " order by from_date, to_date"

                'cmd = New OracleCommand(sql, conn)
                'Using dr = cmd.ExecuteReader()
                '    While dr.Read()

                '        dgvReports.ColumnCount = 14

                '        dgvReports.Columns(0).Name = FROM_DATE
                '        dgvReports.Columns(0).Width = 130

                '        dgvReports.Columns(1).Name = "Έως"
                '        dgvReports.Columns(1).Width = 130

                '        dgvReports.Columns(2).Name = "Χρήστης"
                '        dgvReports.Columns(2).Width = 80

                '        dgvReports.Columns(3).Name = "Αποδείξεις"
                '        dgvReports.Columns(3).Width = 69

                '        dgvReports.Columns(4).Name = "Ποσό 0%"
                '        dgvReports.Columns(4).Width = 40

                '        dgvReports.Columns(5).Name = "Ποσό 3%"
                '        dgvReports.Columns(5).Width = 40

                '        dgvReports.Columns(6).Name = "Ποσό 5%"
                '        dgvReports.Columns(6).Width = 40

                '        dgvReports.Columns(7).Name = "Ποσό 19%"
                '        dgvReports.Columns(7).Width = 40

                '        dgvReports.Columns(8).Name = "Ποσό Πωλήσεων"
                '        dgvReports.Columns(8).Width = 70

                '        dgvReports.Columns(9).Name = "Αρχικό Ποσό"
                '        dgvReports.Columns(9).Width = 60

                '        dgvReports.Columns(10).Name = "Πληρωμές Προμηθευτών"
                '        dgvReports.Columns(10).Width = 80

                '        dgvReports.Columns(11).Name = "Ποσό VISA"
                '        dgvReports.Columns(11).Width = 50

                '        dgvReports.Columns(12).Name = "Τελικό Ποσό Ταμείου για Παράδωση"
                '        dgvReports.Columns(12).Width = 70

                '        dgvReports.Columns(13).Name = "Ποσό Λαχείων για Παράδωση"
                '        dgvReports.Columns(13).Width = 70

                '        'dgvReports.Columns(13).Name = "Αναλυτική Κατάσταση"
                '        'dgvReports.Columns(13).Width = 190

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

                '    dgvReports.ColumnCount = 9

                '    dgvReports.Columns(0).Name = "Z"
                '    dgvReports.Columns(0).Width = 50

                '    dgvReports.Columns(1).Name = FROM_DATE
                '    dgvReports.Columns(1).Width = 250

                '    dgvReports.Columns(2).Name = "Έως"
                '    dgvReports.Columns(2).Width = 250

                '    dgvReports.Columns(3).Name = "Αποδείξεις"
                '    dgvReports.Columns(3).Width = 80

                '    dgvReports.Columns(4).Name = "Ολικό Ποσό 0%"
                '    dgvReports.Columns(4).Width = 90

                '    dgvReports.Columns(5).Name = "Ολικό Ποσό 3%"
                '    dgvReports.Columns(5).Width = 90

                '    dgvReports.Columns(6).Name = "Ολικό Ποσό 5%"
                '    dgvReports.Columns(6).Width = 90

                '    dgvReports.Columns(7).Name = "Ολικό Ποσό 19%"
                '    dgvReports.Columns(7).Width = 90

                '    dgvReports.Columns(8).Name = "Συνολικό Ποσό"
                '    dgvReports.Columns(8).Width = 100

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
                'ClearGridAndSetInvisible()

                'If cmbUsers.SelectedIndex = -1 Then
                '    MessageBox.Show("Δεν έχετε επιλέξει χρήστη", "Επιλογή Χρήστη", MessageBoxButtons.OK, MessageBoxIcon.Error)
                '    Exit Sub
                'End If

                'Dim dateFrom As String = CStr(dtpFrom.Value.Day) & "-" & findMonth(CStr(dtpFrom.Value.Month)) & "-" & CStr(dtpFrom.Value.Year).Substring(2, 2)
                'Dim dateTo As String = CStr(dtpTo.Value.Day) & "-" & findMonth(CStr(dtpTo.Value.Month)) & "-" & CStr(dtpTo.Value.Year).Substring(2, 2)

                'sql = "select login_when, logout_when from sessions " &
                '      "where user_id = '" & userUUIDs(cmbUsers.SelectedIndex) & "' " &
                '      "and login_when BETWEEN " &
                '      "to_timestamp('" & dateFrom & " 00:00:00', 'DD-MON-YY HH24:MI:SS') AND " &
                '      "to_timestamp('" & dateTo & " 23:59:59', 'DD-MON-YY HH24:MI:SS') " &
                '      "order by login_when asc"
                'cmd = New OracleCommand(sql, conn)

                'dgvReports.ColumnCount = 3

                'dgvReports.Columns(0).Name = FROM_DATE
                'dgvReports.Columns(0).Width = 350

                'dgvReports.Columns(1).Name = "Έως"
                'dgvReports.Columns(1).Width = 350

                'dgvReports.Columns(2).Name = "Διάρκεια (σε λεπτά)"
                'dgvReports.Columns(2).Width = 350
                'Dim totalHours As Double = 0
                'Using dr = cmd.ExecuteReader()
                '    While dr.Read
                '        Dim loginWhen As Date = Now
                '        If Not IsDBNull(dr(0)) Then
                '            loginWhen = CDate(dr(0))
                '        End If

                '        Dim logoutWhen As Date = Now
                '        If Not IsDBNull(dr(1)) Then
                '            logoutWhen = CDate(dr(1))
                '        End If

                '        Dim dateDifference As Long = DateDiff(DateInterval.Minute, loginWhen, logoutWhen)
                '        totalHours += (dateDifference / 60)
                '        Dim row As String() = New String() {loginWhen, logoutWhen, dateDifference}
                '        dgvReports.Rows.Add(row)
                '    End While
                'End Using

                'txtBoxTotalHoursOrPayments.Text = totalHours.ToString("N2")
                btnPrint.Visibility = Visibility.Visible

            ElseIf rdbPayments.IsChecked = True Then
                'dgvReports.Columns.Clear()
                'dgvReports.Rows.Clear()
                'Dim dateFrom As String = CStr(dtpFrom.Value.Day) & "-" & findMonth(CStr(dtpFrom.Value.Month)) & "-" & CStr(dtpFrom.Value.Year).Substring(2, 2)
                'Dim dateTo As String = CStr(dtpTo.Value.Day) & "-" & findMonth(CStr(dtpTo.Value.Month)) & "-" & CStr(dtpTo.Value.Year).Substring(2, 2)

                'sql = "select p.CREATED_ON, p.AMOUNT, u.USERNAME, NVL(p.vat, 'N/A'), NVL(p.amountvat,0), NVL(p.vat,-1), " &
                '      "NVL(s.s_name, ' ') s_name, NVL(inv_number, ' ') inv_number " &
                '      "from payments p " &
                '      "inner join users u on p.CREATED_BY = u.UUID " &
                '      "left join suppliers s on p.supplier_id = s.uuid " &
                '      "where p.created_on BETWEEN " &
                '      "to_timestamp('" & dateFrom & " 00:00:00', 'DD-MON-YY HH24:MI:SS') AND " &
                '      "to_timestamp('" & dateTo & " 23:59:59', 'DD-MON-YY HH24:MI:SS') " &
                '      "order by p.created_on desc"
                'cmd = New OracleCommand(sql, conn)
                'Dim totalAmount As Double = 0
                'Dim totalVATamount As Double = 0

                'Dim totalVat0 As Double = 0
                'Dim totalVat3 As Double = 0
                'Dim totalVat5 As Double = 0
                'Dim totalVat19 As Double = 0

                'Dim totalPaymentsVat0 As Double = 0
                'Dim totalPaymentsVat3 As Double = 0
                'Dim totalPaymentsVat5 As Double = 0
                'Dim totalPaymentsVat19 As Double = 0
                'Using dr = cmd.ExecuteReader()
                '    While dr.Read()

                '        dgvReports.ColumnCount = 9

                '        dgvReports.Columns(0).Name = FROM_DATE
                '        dgvReports.Columns(0).Width = 150

                '        dgvReports.Columns(1).Name = "Έως"
                '        dgvReports.Columns(1).Width = 150

                '        dgvReports.Columns(2).Name = "Ημερομηνία Πληρωμής"
                '        dgvReports.Columns(2).Width = 150

                '        dgvReports.Columns(3).Name = "Ποσό"
                '        dgvReports.Columns(3).Width = 50

                '        dgvReports.Columns(4).Name = "Χρήστης"
                '        dgvReports.Columns(4).Width = 100

                '        dgvReports.Columns(5).Name = "Φ.Π.Α"
                '        dgvReports.Columns(5).Width = 80

                '        dgvReports.Columns(6).Name = "Ποσό Φ.Π.Α"
                '        dgvReports.Columns(6).Width = 100

                '        dgvReports.Columns(7).Name = "Προμηθευτής"
                '        dgvReports.Columns(7).Width = 100

                '        dgvReports.Columns(8).Name = "Αρ. Τιμολογίου"
                '        dgvReports.Columns(8).Width = 100

                '        Dim amount As Double = 0
                '        amount = CDbl(dr(1))
                '        totalAmount += amount
                '        totalVATamount += CDbl(dr(4))

                '        If CInt(dr(5)) = 0 Then
                '            totalVat0 += CDbl(dr(4))
                '            totalPaymentsVat0 += amount
                '        ElseIf CInt(dr(5)) = 3 Then
                '            totalVat3 += CDbl(dr(4))
                '            totalPaymentsVat3 += amount
                '        ElseIf CInt(dr(5)) = 5 Then
                '            totalVat5 += CDbl(dr(4))
                '            totalPaymentsVat5 += amount
                '        ElseIf CInt(dr(5)) = 19 Then
                '            totalVat19 += CDbl(dr(4))
                '            totalPaymentsVat19 += amount
                '        End If

                '        Dim row As String() = New String() {dateFrom, dateTo, CStr(dr(0)), amount.ToString("N2"), CStr(dr(2)), dr(3), dr(4), dr(6), dr(7)}
                '        dgvReports.Rows.Add(row)
                '    End While
                'End Using

                'txtBoxTotalHoursOrPayments.Text = "€" & TruncateDecimal(totalAmount + totalVATamount, 3).ToString
                'lblAmountVAT.Text = "Φ.Π.Α για Επιστροφή: €" + TruncateDecimal(totalVATamount, 3).ToString + vbNewLine +
                '"Πληρωμές (με Φ.Π.Α) 0% : " + TruncateDecimal(totalPaymentsVat0 + totalVat0, 3).ToString + " , Φ.Π.Α. 0%: " + TruncateDecimal(totalVat0, 3).ToString + vbNewLine +
                '"Πληρωμές (με Φ.Π.Α) 3% : " + TruncateDecimal(totalPaymentsVat3 + totalVat3, 3).ToString + " , Φ.Π.Α. 3%: " + TruncateDecimal(totalVat3, 3).ToString + vbNewLine +
                '"Πληρωμές (με Φ.Π.Α) 5% : " + TruncateDecimal(totalPaymentsVat5 + totalVat5, 3).ToString + " , Φ.Π.Α. 5%: " + TruncateDecimal(totalVat5, 3).ToString + vbNewLine +
                '"Πληρωμές (με Φ.Π.Α) 19%: " + TruncateDecimal(totalPaymentsVat19 + totalVat19, 3).ToString + ", Φ.Π.Α. 19%: " + TruncateDecimal(totalVat19, 3).ToString
                btnPrint.Visibility = Visibility.Visible

            ElseIf rdbQntHistory.IsChecked = True Then
                'dgvReports.Columns.Clear()
                'dgvReports.Rows.Clear()

                'Dim dateFrom As String = CStr(dtpFrom.Value.Day) & "-" & findMonth(CStr(dtpFrom.Value.Month)) & "-" & CStr(dtpFrom.Value.Year).Substring(2, 2)
                'Dim dateTo As String = CStr(dtpTo.Value.Day) & "-" & findMonth(CStr(dtpTo.Value.Month)) & "-" & CStr(dtpTo.Value.Year).Substring(2, 2)

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
                'dgvReports.ColumnCount = 10

                'dgvReports.Columns(0).Name = "Barcode"
                'dgvReports.Columns(0).Width = 100

                'dgvReports.Columns(1).Name = "Προϊόν"
                'dgvReports.Columns(1).Width = 130

                'dgvReports.Columns(2).Name = "Προηγούμενη Ποσότητα"
                'dgvReports.Columns(2).Width = 80

                'dgvReports.Columns(3).Name = "Νέα Ποσότητα"
                'dgvReports.Columns(3).Width = 80

                'dgvReports.Columns(4).Name = "Προηγ.Ποσ. Αποθήκης"
                'dgvReports.Columns(4).Width = 80

                'dgvReports.Columns(5).Name = "Νέα Ποσ. Αποθήκης"
                'dgvReports.Columns(5).Width = 100

                'dgvReports.Columns(6).Name = "Ημερομηνία Αλλαγής"
                'dgvReports.Columns(6).Width = 130

                'dgvReports.Columns(7).Name = "Χρήστης Αλλαγής"
                'dgvReports.Columns(7).Width = 80

                'dgvReports.Columns(8).Name = "Προηγ. Τιμή"
                'dgvReports.Columns(8).Width = 80

                'dgvReports.Columns(9).Name = "Νέα Τιμή"
                'dgvReports.Columns(9).Width = 100
                'Using dr = cmd.ExecuteReader()
                '    While dr.Read()
                '        Dim row As String() = New String() {dr(0), dr(1), dr(2), dr(3), dr(4), dr(5), dr(6), dr(7), dr(8), dr(9)}
                '        dgvReports.Rows.Add(row)
                '    End While
                'End Using

                btnPrint.Visibility = Visibility.Visible 

            ElseIf rdbSessions.IsChecked = True Then
                dgvReports.Columns.Clear()
                dgvReports.Items.Clear()

                '--- Columns
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Χρήστης", .Binding = New Binding("Username")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Από", .Binding = New Binding("LoginWhen")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Έως", .Binding = New Binding("LogoutWhen")})
                dgvReports.Columns.Add(New DataGridTextColumn With {.Header = "Μηχανή", .Binding = New Binding("Machine")})

                Dim dateFrom = dtpFrom.SelectedDate
                Dim dateTo = dtpTo.SelectedDate

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
