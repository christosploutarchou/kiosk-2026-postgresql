Imports System.Data
Imports System.Drawing.Printing

Public Class AdminWindow

    Private Sub AdminWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        ' --- Admin permissions ---
        If currentUser.isAdmin Then
            btnUsers.IsEnabled = True
            btnCategories.IsEnabled = True
            btnProducts.IsEnabled = True
            btnReceipts.IsEnabled = True
            btnReports.IsEnabled = True
            btnUpdate.IsEnabled = True
            btnBackup.IsEnabled = True
            btnSuppliers.IsEnabled = True
            btnLottery.IsEnabled = True

            txtBoxInitialFiscalAmt.IsReadOnly = False
            btnMessages.Visibility = Visibility.Visible
            dgvMessages.Visibility = Visibility.Visible
        Else
            btnUsers.IsEnabled = False
            btnSuppliers.IsEnabled = False
            btnCategories.IsEnabled = False
            btnProducts.IsEnabled = False
            btnReceipts.IsEnabled = False
            btnReports.IsEnabled = False
            btnUpdate.IsEnabled = False
            btnBackup.IsEnabled = False
            btnLottery.IsEnabled = False

            txtBoxInitialFiscalAmt.IsReadOnly = True
            btnMessages.Visibility = Visibility.Collapsed
            dgvMessages.Visibility = Visibility.Collapsed
        End If

        ' --- Report permission ---
        btnReports.IsEnabled = currentUser.canViewReports

        ' --- Product permissions ---
        If currentUser.canEditProducts OrElse currentUser.canEditProductsFull Then
            btnProducts.IsEnabled = True
            btnSuppliers.IsEnabled = True
            btnReports.IsEnabled = True
        End If

        ' --- User info ---
        txtBoxUser.Text = username

        ' --- Load data ---
        CheckMessages()
        GetInitialAmt()

        ' --- Context menus ---
        dgvMessages.ContextMenu = CType(Resources("MessagesContextMenu"), ContextMenu)
        dgvExpiry.ContextMenu = CType(Resources("ExpiryContextMenu"), ContextMenu)

    End Sub

    Private Sub ContextMenu_Copy_Click(sender As Object, e As RoutedEventArgs)
        If dgvMessages.SelectedItem IsNot Nothing Then
            Clipboard.SetText(dgvMessages.SelectedItem.ToString())
        End If
    End Sub

    Private Sub ContextMenuMessages_Copy_Click(sender As Object, e As RoutedEventArgs)
        If dgvExpiry.SelectedItem IsNot Nothing Then
            Clipboard.SetText(dgvExpiry.SelectedItem.ToString())
        End If
    End Sub

    'Private Sub btnExit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnExit.Click
    'If MessageBox.Show("Εξοδος;", "Εξοδος", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = Windows.Forms.DialogResult.Yes Then
    'If GenerateXreport(whois) Then
    'If MessageBox.Show("Εκτύπωση Αναφοράς Βάρδιας;", "Εκτύπωση", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = Windows.Forms.DialogResult.Yes Then
    '               PrintDocument1.PrinterSettings.Copies = 1
    '              PrintDocument1.Print()
    'End If
    'End If
    '       logoutUserUUID(whois)
    'Me.Dispose()
    '       frmLogin.Dispose()
    ''frmLogin.Show()
    'End If
    'End Sub

    Private Sub BtnUsers_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUsers.Click
        'If Not isLoggedIn(username) Then
        'MessageBox.Show("Ο χρήστης δεν ειναι συνδεμένος", "Σφάλμα", MessageBoxButtons.OK, MessageBoxIcon.Error)
        'Exit Sub
        'End If
        'Me.Hide()
        'frmNewUser.Show()
    End Sub

    'Private Sub frmMain_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
    '    btnExit_Click(sender, e)
    'End Sub

    'Private Sub contextmenu_click(ByVal sender As System.Object, ByVal e As ToolStripItemClickedEventArgs)
    'Dim clickCell As DataGridViewCell = dgvMessages.SelectedCells(0)
    'Select Case e.ClickedItem.Text
    'Case "Copy"
    '           Clipboard.SetText(clickCell.Value, TextDataFormat.Text)
    'End Select
    'End Sub

    'Private Sub contextmenuMessages_click(ByVal sender As System.Object, ByVal e As ToolStripItemClickedEventArgs)
    'Dim clickCell As DataGridViewCell = dgvExpiry.SelectedCells(0)
    'Select Case e.ClickedItem.Text
    'Case "Copy"
    '           Clipboard.SetText(clickCell.Value, TextDataFormat.Text)
    'End Select
    'End Sub

    'Private Sub dgvMessages_CellMouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellMouseEventArgs) Handles dgvMessages.CellMouseDown
    'If e.RowIndex <> -1 And e.ColumnIndex <> -1 Then
    'If e.Button = MouseButtons.Right Then
    'Dim clickCell As DataGridViewCell = sender.Rows(e.RowIndex).Cells(e.ColumnIndex)
    '           clickCell.Selected = True
    'End If
    'End If
    'End Sub

    'Private Sub dgvExpiry_CellMouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellMouseEventArgs) Handles dgvExpiry.CellMouseDown
    'If e.RowIndex <> -1 And e.ColumnIndex <> -1 Then
    'If e.Button = MouseButtons.Right Then
    'Dim clickCell As DataGridViewCell = sender.Rows(e.RowIndex).Cells(e.ColumnIndex)
    '           clickCell.Selected = True
    'End If
    'End If
    'End Sub

    'Private Sub cmdSuppliers_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdSuppliers.Click
    '    If Not isLoggedIn(username) Then
    '           MessageBox.Show("Ο χρήστης δεν ειναι συνδεμένος", "Σφάλμα", MessageBoxButtons.OK, MessageBoxIcon.Error)
    '  Exit Sub
    ' End If
    'Me.Hide()
    '   frmSuppliers.Show()
    'End Sub

    'Private Sub btnProducts_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnProducts.Click
    'If Not isLoggedIn(username) Then
    '       MessageBox.Show("Ο χρήστης δεν ειναι συνδεμένος", "Σφάλμα", MessageBoxButtons.OK, MessageBoxIcon.Error)
    'Exit Sub
    'End If
    'Me.Hide()
    '   frmProducts.Show()
    'End Sub

    Private Sub CheckMessages()
        Dim WhoAmI As String = "CheckMessages"
        Dim sql As String = ""

        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                ' =========================
                ' MIN QUANTITY ALERTS
                ' =========================
                sql =
                "SELECT p.description, b.barcode, p.min_quantity, p.avail_quantity, s.s_name " &
                "FROM products p " &
                "JOIN barcodes b ON p.serno = b.product_serno " &
                "JOIN suppliers s ON p.supplier_id = s.uuid " &
                "WHERE p.kioskid = @kioskid " &
                "  AND p.alert_on_min = 1 " &
                "  AND p.avail_quantity <= p.min_quantity " &
                "ORDER BY p.description ASC"

                Dim minList As New List(Of Object)
                Using cmd As New Npgsql.NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)

                    Using dr = cmd.ExecuteReader()
                        Dim i As Integer = 1
                        While dr.Read()
                            minList.Add(New With {
                            .Index = i,
                            .Description = dr.GetString(0),
                            .Barcode = dr.GetString(1),
                            .MinQty = dr.GetInt32(2),
                            .AvailQty = dr.GetInt32(3),
                            .Supplier = dr.GetString(4)
                        })
                            i += 1
                        End While
                    End Using
                End Using
                dgvMessages.ItemsSource = minList


                ' =========================
                ' EXPIRY ALERTS
                ' =========================
                sql =
                "SELECT p.description, b.barcode, p.expiry_date, s.s_name " &
                "FROM products p " &
                "JOIN barcodes b ON p.serno = b.product_serno " &
                "JOIN suppliers s ON p.supplier_id = s.uuid " &
                "WHERE p.kioskid = @kioskid " &
                "  AND p.alert_on_expiry = 1 " &
                "  AND p.alert_date <= CURRENT_DATE"

                Dim expiryList As New List(Of Object)
                Using cmd As New Npgsql.NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)

                    Using dr = cmd.ExecuteReader()
                        Dim i As Integer = 1
                        While dr.Read()
                            ' Nullable-safe expiry date
                            Dim expiryDate As DateTime? = Nothing
                            If Not dr.IsDBNull(2) Then
                                Dim tmpDate = dr.GetDateTime(2)
                                ' Ensure date is within .NET valid range
                                If tmpDate.Year >= 1 AndAlso tmpDate.Year <= 9999 Then
                                    expiryDate = tmpDate
                                End If
                            End If

                            expiryList.Add(New With {
                            .Index = i,
                            .Description = If(dr.IsDBNull(0), "", dr.GetString(0)),
                            .Barcode = If(dr.IsDBNull(1), "", dr.GetString(1)),
                            .ExpiryDate = expiryDate,
                            .Supplier = If(dr.IsDBNull(3), "", dr.GetString(3))
                        })
                            i += 1
                        End While
                    End Using
                End Using
                dgvExpiry.ItemsSource = expiryList


                ' =========================
                ' SUPPLIERS BY DAY
                ' =========================
                Dim dayColumn As String = Date.Today.DayOfWeek.ToString().Substring(0, 3).ToUpper()

                If Not dayColumn.Equals("SAT") And Not dayColumn.Equals("SUN") Then
                    sql =
                            $"SELECT s_name, contact_name, phone_1, notes " &
                            $"FROM suppliers " &
                            $"WHERE kioskid = @kioskid AND {dayColumn} = 1 " &
                            $"ORDER BY s_name ASC"

                    Dim supplierList As New List(Of Object)
                    Using cmd As New Npgsql.NpgsqlCommand(sql, conn)
                        cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)

                        Using dr = cmd.ExecuteReader()
                            Dim i As Integer = 1
                            While dr.Read()
                                supplierList.Add(New With {
                                .Index = i,
                                .Supplier = dr.GetString(0),
                                .Contact = dr.GetString(1),
                                .Phone = dr.GetString(2),
                                .Notes = dr.GetString(3)
                            })
                                i += 1
                            End While
                        End Using
                    End Using
                    dgvSuppliers.ItemsSource = supplierList
                End If
            End Using
        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", sql)
            MessageBox.Show(ex.Message, "Application Error",
                        MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


    Private Sub BtnUpdate_Click(sender As Object, e As RoutedEventArgs) Handles btnUpdate.Click
        Dim WhoAmI As String = "BtnUpdate_Click"

        ' Validate numeric input (culture-safe)
        Dim initialAmt As Decimal
        If Not Decimal.TryParse(
        txtBoxInitialFiscalAmt.Text,
        Globalization.NumberStyles.Any,
        Globalization.CultureInfo.CurrentCulture,
        initialAmt) Then

            MessageBox.Show(
            "Το πεδίο 'Αρχικό Ποσό Ταμείου' πρέπει να είναι αριθμός",
            "Σφάλμα",
            MessageBoxButton.OK,
            MessageBoxImage.Error)
            Exit Sub
        End If

        Dim sql As String =
        "UPDATE global_params " &
        "SET paramvalue = @value " &
        "WHERE kioskid = @kioskid AND paramkey = 'init.fiscal.amt'"

        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New Npgsql.NpgsqlCommand(sql, conn)
                    cmd.CommandType = CommandType.Text
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)
                    cmd.Parameters.Add("@value", NpgsqlTypes.NpgsqlDbType.Numeric).Value = initialAmt
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            MessageBox.Show(
            "Τα ποσά έχουν αποθηκευτεί επιτυχώς",
            "Αποθήκευση αλλαγής",
            MessageBoxButton.OK,
            MessageBoxImage.Information)

        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", sql)
            MessageBox.Show(
            ex.Message,
            "Application Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error)
        End Try
    End Sub


    Private Sub GetInitialAmt()
        Dim WhoAmI As String = "GetInitialAmt"
        Dim sql As String = "SELECT paramvalue FROM global_params WHERE kioskid = @kioskid AND paramkey = 'init.fiscal.amt'"

        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New Npgsql.NpgsqlCommand(sql, conn)
                    cmd.CommandType = CommandType.Text
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)

                    Dim initialAmount As Double = 0

                    Using dr = cmd.ExecuteReader()
                        If dr.Read() AndAlso Not dr.IsDBNull(0) Then
                            initialAmount = Convert.ToDouble(dr(0))
                        End If
                    End Using

                    txtBoxInitialFiscalAmt.Text = initialAmount.ToString("N2")
                End Using
            End Using

        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", sql)
            MessageBox.Show(
                ex.Message,
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            )
        End Try
    End Sub

    Private Sub BtnMessages_Click(sender As Object, e As RoutedEventArgs) Handles btnMessages.Click
        CheckMessages()
    End Sub

    Private Sub btnExit_Click(sender As Object, e As RoutedEventArgs) Handles btnExit.Click
        If MessageBox.Show("Εξοδος;", "Εξοδος", MessageBoxButton.YesNo, MessageBoxImage.Question) = System.Windows.MessageBoxResult.Yes Then
            If GenerateXreport(Guid.Parse(currentUserID)) Then
                If MessageBox.Show("Εκτύπωση Αναφοράς Βάρδιας;", "Εκτύπωση", MessageBoxButton.YesNo, MessageBoxImage.Question) = System.Windows.MessageBoxResult.Yes Then
                    PrintXReportSilent(Guid.Parse(currentUserID), username)
                End If
            End If
            LogoutCurrentUser()
            Me.Close()
            Application.Current.MainWindow.Close()
        End If
    End Sub

    Private Sub BtnCategories_Click(sender As Object, e As RoutedEventArgs) Handles btnCategories.Click
        Me.Hide()
        Dim categoriesWindow As New CategoriesWindow
        categoriesWindow.Show()
    End Sub



    'Private Sub btnPos_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnPos.Click
    '    If Not isLoggedIn(username) Then
    '        MessageBox.Show("Ο χρήστης δεν ειναι συνδεμένος", "Σφάλμα", MessageBoxButtons.OK, MessageBoxIcon.Error)
    '        Exit Sub
    '    End If
    '    Me.Hide()
    '    frmPOS.Show()
    '    If dualMonitor Then
    '        frmDual.Show()
    '    End If
    'End Sub

    'Private Sub btnReports_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnReports.Click
    '    If Not isLoggedIn(username) Then
    '        MessageBox.Show("Ο χρήστης δεν ειναι συνδεμένος", "Σφάλμα", MessageBoxButtons.OK, MessageBoxIcon.Error)
    '        Exit Sub
    '    End If
    '    Me.Hide()
    '    frmReports.Show()
    'End Sub

    'Private Sub btnReceipts_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnReceipts.Click
    '    If Not isLoggedIn(username) Then
    '        MessageBox.Show("Ο χρήστης δεν ειναι συνδεμένος", "Σφάλμα", MessageBoxButtons.OK, MessageBoxIcon.Error)
    '        Exit Sub
    '    End If
    '    Me.Hide()
    '    frmReceipts.Show()
    'End Sub

    'Private Sub PrintDocument1_PrintPage(ByVal sender As System.Object, ByVal e As System.Drawing.Printing.PrintPageEventArgs) Handles PrintDocument1.PrintPage
    '    Dim headerFont As Font = New Drawing.Font(REPORT_FONT, 15, FontStyle.Bold)
    '    Dim reportFont As Font = New Drawing.Font(REPORT_FONT, 9)
    '    Dim reportFontSmall As Font = New Drawing.Font(REPORT_FONT, 9)

    '    e.Graphics.DrawString(KIOSK_NAME, headerFont, Brushes.Black, 65, 0)
    '    e.Graphics.DrawString(COMPANY_NAME, reportFont, Brushes.Black, 95, 35)
    '    e.Graphics.DrawString(KIOSK_ADDRESS1, reportFont, Brushes.Black, 75, 50)
    '    e.Graphics.DrawString(KIOSK_ADDRESS2, reportFont, Brushes.Black, 95, 65)
    '    e.Graphics.DrawString(COMPANY_VAT, reportFont, Brushes.Black, 60, 80)
    '    e.Graphics.DrawString(SINGE_DASHED_LINE, reportFont, Brushes.Black, 0, 95)
    '    e.Graphics.DrawString("Date: " & DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), reportFont, Brushes.Black, 0, 110)
    '    e.Graphics.DrawString(SINGE_DASHED_LINE, reportFont, Brushes.Black, 0, 130)
    '    e.Graphics.DrawString("Χρήστης: " & getUser(whois), reportFont, Brushes.Black, 0, 160)

    '    Dim cmd As New OracleCommand("", conn)
    '    Dim dr As OracleDataReader
    '    Dim sql As String = ""
    '    Try
    '        sql = "select from_date, to_date, total_receipts, total5percent, total19percent, payments, " &
    '              "initial_amt, final_amt, description, total0percent, amount_laxeia, initialAmtLaxeia, amountVisa, NVL(finalAmtLaxeia,0), total3percent " &
    '              "from x_report " &
    '              "where user_id = '" & whois & "' and created_on = (select max(created_on) from x_report)"

    '        cmd = New OracleCommand(sql, conn)
    '        dr = cmd.ExecuteReader()

    '        Dim xMargin As Integer = 180
    '        If dr.Read Then
    '            e.Graphics.DrawString("Από: " & CStr(dr(0)), reportFont, Brushes.Black, 0, xMargin)
    '            xMargin += 20
    '            e.Graphics.DrawString("Έως: " & CStr(dr(1)), reportFont, Brushes.Black, 0, xMargin)

    '            If isAdmin Then
    '                xMargin += 20
    '                e.Graphics.DrawString("Αρ. Αποδείξεων: " & CStr(dr(2)), reportFont, Brushes.Black, 0, xMargin)
    '            End If

    '            Dim totalVat5 As Double = CDbl(dr(3))
    '            Dim totalVat19 As Double = CDbl(dr(4))
    '            Dim payments As Double = CDbl(dr(5))
    '            Dim initial As Double = CDbl(dr(6))
    '            Dim final As Double = CDbl(dr(7))
    '            Dim totalVat0 As Double = CDbl(dr(9))
    '            Dim amountLaxeia As Double = CDbl(dr(10))
    '            Dim initialAmountLaxeia As Double = CDbl(dr(11))
    '            Dim amountVisa As Double = CDbl(dr(12))
    '            Dim finalAmtLaxeia As Double = CDbl(dr(13))
    '            Dim totalVat3 As Double = CDbl(dr(14))

    '            Dim totalReceivedAmt As Double = totalVat0 + totalVat3 + totalVat5 + totalVat19
    '            Dim totalAmountToDeliver = (totalReceivedAmt + initial) - payments - amountVisa

    '            If isAdmin Then
    '                xMargin += 20
    '                e.Graphics.DrawString("Φ.Π.Α. 0%: " & totalVat0.ToString("N2"), reportFont, Brushes.Black, 0, xMargin)
    '                xMargin += 20
    '                e.Graphics.DrawString("Φ.Π.Α. 3%: " & totalVat3.ToString("N2"), reportFont, Brushes.Black, 0, xMargin)
    '                xMargin += 20
    '                e.Graphics.DrawString("Φ.Π.Α. 5%: " & totalVat5.ToString("N2"), reportFont, Brushes.Black, 0, xMargin)
    '                xMargin += 20
    '                e.Graphics.DrawString("Φ.Π.Α. 19%: " & totalVat19.ToString("N2"), reportFont, Brushes.Black, 0, xMargin)
    '                xMargin += 20
    '                e.Graphics.DrawString("Πληρωμές: " & payments.ToString("N2"), reportFont, Brushes.Black, 0, xMargin)
    '            End If

    '            xMargin += 20
    '            e.Graphics.DrawString("Αρχικό Ποσό: " & initial.ToString("N2"), reportFont, Brushes.Black, 0, xMargin)

    '            If isAdmin Then
    '                xMargin += 20
    '                e.Graphics.DrawString("Ποσό Είσπραξης: " & totalReceivedAmt.ToString("N2"), reportFont, Brushes.Black, 0, xMargin)
    '                xMargin += 20
    '                e.Graphics.DrawString("Ποσό VISA: " & amountVisa.ToString("N2"), reportFont, Brushes.Black, 0, xMargin)
    '                xMargin += 20
    '                e.Graphics.DrawString("Τελικό Ποσό Ταμείου για Παράδοση: " & totalAmountToDeliver.ToString("N2"), reportFont, Brushes.Black, 0, xMargin)
    '            End If

    '            xMargin += 20
    '            e.Graphics.DrawString("Ποσο λαχείων για Παράδοση: " & (finalAmtLaxeia).ToString("N2"), reportFont, Brushes.Black, 0, xMargin)

    '            If Not dr.IsDBNull(8) Then
    '                e.Graphics.DrawString(dr(8), reportFont, Brushes.Black, 0, xMargin)
    '            End If
    '        End If
    '        dr.Close()
    '    Catch ex As Exception
    '        CreateExceptionFile(ex.Message, " " & sql)
    '        MessageBox.Show(ex.Message, APPLICATION_ERROR, MessageBoxButtons.OK, MessageBoxIcon.Error)
    '    Finally
    '        cmd.Dispose()
    '    End Try
    'End Sub

    'Private Sub btnBackup_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBackup.Click
    '    Dim cmd As New OracleCommand("", conn)
    '    Try
    '        Dim path = "C:\"
    '        cmd = New OracleCommand(Q_EXPORT_DB, conn)
    '        cmd.CommandType = CommandType.Text
    '        Dim dr As OracleDataReader = cmd.ExecuteReader()
    '        If dr.Read Then
    '            path = CStr(dr(0))
    '        End If
    '        dr.Close()
    '        Dim fileName As String = Date.Now.Month.ToString & Date.Now.Day.ToString & Date.Now.Year.ToString &
    '                                 Date.Now.Hour.ToString & Date.Now.Minute.ToString & Date.Now.Millisecond.ToString

    '        Shell("exp kiosk/oracle@orcl buffer=4096 grants=Y file=" & path & "backup" & fileName & ".dmp", vbNormalFocus)
    '    Catch ex As Exception
    '        CreateExceptionFile(ex.Message, " " & Q_EXPORT_DB)
    '        MessageBox.Show(CANNOT_EXPORT_DB_FROM_THIS_TERMINAL, APPLICATION_ERROR, MessageBoxButtons.OK, MessageBoxIcon.Warning)
    '    Finally
    '        cmd.Dispose()
    '    End Try
    'End Sub

    'Protected Overrides ReadOnly Property CreateParams() As CreateParams
    '    'Disables X button
    '    Get
    '        Dim param As CreateParams = MyBase.CreateParams
    '        param.ClassStyle = param.ClassStyle Or &H200
    '        Return param
    '    End Get
    'End Property

    'Private Sub txtBoxInitialFiscalAmt_MouseEnter(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtBoxInitialFiscalAmt.MouseEnter
    '    txtBoxInitialFiscalAmt.BackColor = Color.Bisque
    '    txtBoxInitialFiscalAmt.Focus()
    'End Sub

    'Private Sub txtBoxInitialFiscalAmt_MouseLeave(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtBoxInitialFiscalAmt.MouseLeave
    '    txtBoxInitialFiscalAmt.BackColor = Color.LemonChiffon
    'End Sub

    'Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnInvoices.Click
    '    If Not isLoggedIn(username) Then
    '        MessageBox.Show("Ο χρήστης δεν ειναι συνδεμένος", "Σφάλμα", MessageBoxButtons.OK, MessageBoxIcon.Error)
    '        Exit Sub
    '    End If
    '    Me.Hide()
    '    frmInvoices.Show()
    'End Sub

    'Private Sub btnEditPos_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEditPos.Click
    '    If Not isLoggedIn(username) Then
    '        MessageBox.Show("Ο χρήστης δεν ειναι συνδεμένος", "Σφάλμα", MessageBoxButtons.OK, MessageBoxIcon.Error)
    '        Exit Sub
    '    End If
    '    Me.Hide()
    '    frmPOSEdit.Show()
    'End Sub

    'Private Sub btnLottery_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnLottery.Click
    '    If Not isLoggedIn(username) Then
    '        MessageBox.Show("Ο χρήστης δεν ειναι συνδεμένος", "Σφάλμα", MessageBoxButtons.OK, MessageBoxIcon.Error)
    '        Exit Sub
    '    End If
    '    Me.Hide()
    '    frmLottery.Show()
    'End Sub
End Class
