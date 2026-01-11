Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Data
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text.Encodings
Imports System.Windows.Interop
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
        setVisibleFields(CInt(rb.Tag))
    End Sub


    Private Sub setVisibleFields(ByVal reportType As String)
        clearGridAndSetInvisible()
        lblTotalSalesAmount.ResetText()
        chkBoxSalesPerSupplier.Checked = False
        cmbCategories.Visible = False

        If reportType.Equals(QNT_HISTORY) Then
            txtBoxBarcode.Visible = False
            txtBoxBarcode.Visible = False
            btnClearBarcode.Visible = False
            cmbNoBarcode.Visible = False
            lblBarcode.Visible = False
        End If

        If reportType.Equals(PRODUCTS_PER_SUPPLIER) Then
            chkBoxSalesPerSupplier.Visible = True
            cmbSupplier.Visible = True
            fillSuppliers()
        Else
            chkBoxSalesPerSupplier.Visible = False
            cmbSupplier.Visible = False
        End If

        If reportType.Equals(PAYMENTS) And isAdmin Then
            lblAmountVAT.Visible = True
        Else
            lblAmountVAT.Visible = False
            lblAmountVAT.Text = ""
        End If

        If reportType.Equals(QNT_HISTORY) Or reportType.Equals(SALES_PER_VAT) Or reportType.Equals(X_REPORT) Or
           reportType.Equals(Z_REPORT) Or reportType.Equals(HOURS_PER_USER) Or reportType.Equals(PAYMENTS) Or
            reportType.Equals(LOGIN_HISTORY) Or reportType.Equals(SALES_PER_CATEGORY) Then
            btnSearch.Visible = True
        Else
            btnSearch.Visible = False
        End If

        If reportType.Equals(QNT_HISTORY) Or reportType.Equals(SALES_PER_PRODUCT) Or reportType.Equals(SALES_PER_VAT) Or
           reportType.Equals(X_REPORT) Or reportType.Equals(Z_REPORT) Or reportType.Equals(HOURS_PER_USER) Or reportType.Equals(PAYMENTS) _
           Or reportType.Equals(LOGIN_HISTORY) Or reportType.Equals(SALES_PER_CATEGORY) Then
            showDateFields(True)
        Else
            showDateFields(False)
        End If

        If reportType.Equals(QUANTITY_PER_PRODUCT) Or reportType.Equals(BUY_SELL) Then
            btnPrint.Visible = False
            'btnExportToExcel.Visible = False
            cmbUsers.Visible = False
            lblTotalHoursOrAmount.Visible = False
            txtBoxTotalHoursOrPayments.Visible = False
            cmbNoBarcode.Visible = False

            btnClearBarcode.Visible = True
            lblBarcode.Visible = True
            txtBoxBarcode.Visible = True
            txtBoxBarcode.Focus()
            txtBoxBarcode.Visible = True
        End If

        If reportType.Equals(SALES_PER_PRODUCT) Then
            fillProductsNoBarcode()
            btnPrint.Visible = False
            'btnExportToExcel.Visible = False
            cmbUsers.Visible = False
            lblTotalHoursOrAmount.Visible = False
            txtBoxTotalHoursOrPayments.Visible = False
            lblAmountVAT.Visible = True
            lblBarcode.Visible = True
            txtBoxBarcode.Visible = True
            txtBoxBarcode.Focus()
            txtBoxBarcode.Visible = True
            btnClearBarcode.Visible = True
            cmbNoBarcode.Visible = True

        ElseIf reportType.Equals(PAYMENTS) Then
            lblBarcode.Visible = False
            txtBoxBarcode.Visible = False
            btnClearBarcode.Visible = False
            btnPrint.Visible = False
            'btnExportToExcel.Visible = False
            cmbUsers.Visible = False
            lblTotalHoursOrAmount.Visible = False
            txtBoxTotalHoursOrPayments.Visible = False
            cmbNoBarcode.Visible = False

            lblTotalHoursOrAmount.Visible = True
            txtBoxTotalHoursOrPayments.Visible = True
            txtBoxTotalHoursOrPayments.Text = "0"
            lblTotalHoursOrAmount.Text = "Σύνολο"

        ElseIf reportType.Equals(SALES_PER_VAT) Or reportType.Equals(Z_REPORT) Then
            lblBarcode.Visible = False
            txtBoxBarcode.Visible = False
            btnClearBarcode.Visible = False
            btnPrint.Visible = False
            'btnExportToExcel.Visible = False
            cmbUsers.Visible = False
            lblTotalHoursOrAmount.Visible = False
            txtBoxTotalHoursOrPayments.Visible = False
            cmbNoBarcode.Visible = False

        ElseIf reportType.Equals(X_REPORT) Then
            btnPrint.Visible = False
            'btnExportToExcel.Visible = False
            lblBarcode.Visible = False
            txtBoxBarcode.Visible = False
            txtBoxBarcode.Visible = False
            btnClearBarcode.Visible = False
            lblTotalHoursOrAmount.Visible = False
            txtBoxTotalHoursOrPayments.Visible = False
            cmbNoBarcode.Visible = False
            cmbUsers.Visible = True
            fillUsers(1)

        ElseIf reportType.Equals(PRODUCTS_PER_SUPPLIER) Then
            lblBarcode.Visible = False
            txtBoxBarcode.Visible = False
            btnClearBarcode.Visible = False
            btnPrint.Visible = False
            'btnExportToExcel.Visible = False
            cmbUsers.Visible = False
            lblTotalHoursOrAmount.Visible = False
            txtBoxTotalHoursOrPayments.Visible = False
            cmbNoBarcode.Visible = False

        ElseIf reportType.Equals(HOURS_PER_USER) Then
            cmbUsers.Visible = True
            fillUsers(-1)
            lblTotalHoursOrAmount.Visible = True
            txtBoxTotalHoursOrPayments.Visible = True
            txtBoxTotalHoursOrPayments.Text = "0"
            lblTotalHoursOrAmount.Text = "Σύνολο Ωρών"

            lblBarcode.Visible = False
            txtBoxBarcode.Visible = False
            btnClearBarcode.Visible = False
            btnPrint.Visible = False
            'btnExportToExcel.Visible = False
            cmbNoBarcode.Visible = False

        ElseIf reportType.Equals(LOGIN_HISTORY) Then
            btnPrint.Visible = False
            'btnExportToExcel.Visible = False
            lblBarcode.Visible = False
            txtBoxBarcode.Visible = False
            txtBoxBarcode.Visible = False
            btnClearBarcode.Visible = False
            lblTotalHoursOrAmount.Visible = False
            txtBoxTotalHoursOrPayments.Visible = False
            cmbNoBarcode.Visible = False
            cmbUsers.Visible = False
            'fillUsers(1)

        ElseIf reportType.Equals(SALES_PER_CATEGORY) Then
            btnPrint.Visible = False
            'btnExportToExcel.Visible = False
            lblBarcode.Visible = False
            txtBoxBarcode.Visible = False
            txtBoxBarcode.Visible = False
            btnClearBarcode.Visible = False
            lblTotalHoursOrAmount.Visible = False
            txtBoxTotalHoursOrPayments.Visible = False
            cmbNoBarcode.Visible = False
            cmbUsers.Visible = False
            cmbCategories.Visible = True
            fillCategories(1)
        End If
    End Sub



End Class
