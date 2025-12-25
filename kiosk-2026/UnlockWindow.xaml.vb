Imports Npgsql
Imports System.Printing
Imports System.Windows.Xps
Imports System.Windows.Xps.Packaging
Imports System.Globalization

Class UnlockWindow
    Dim userId As Guid
    Private Sub UnlockWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        FillUsers()
    End Sub

    Private Sub FillUsers()
        Dim WhoAmI As String = "FillUsers"
        Dim sql As String =
                "SELECT u.username " &
                "FROM sessions s " &
                "INNER JOIN users u ON u.uuid = s.user_id " &
                "WHERE u.kioskid = @kioskid " &
                "AND s.is_active = TRUE"

        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()
                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value =
                    Guid.Parse(kioskid)

                    Using reader = cmd.ExecuteReader()
                        lstboxLockedUsers.Items.Clear()
                        While reader.Read()
                            lstboxLockedUsers.Items.Add(reader.GetString(0))
                        End While
                    End Using
                End Using
            End Using

        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", sql)
            MessageBox.Show(
            "Error loading users: " & ex.Message,
            "Database Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        )
        End Try
    End Sub

    Private Sub BtnExit_Click(sender As Object, e As RoutedEventArgs) Handles btnExit.Click
        Me.Close()
    End Sub

    Private Sub BtnUnlock_Click(sender As Object, e As RoutedEventArgs) Handles btnUnlock.Click
        Dim WhoAmI As String = "BtnUnlock_Click"
        Dim sql As String = ""
        If lstboxLockedUsers.SelectedIndex = -1 Then
            MessageBox.Show("Select a user to unlock", "Select user", MessageBoxButton.OK, MessageBoxImage.Information)
            Exit Sub
        End If
        Try
            sql = "SELECT uuid FROM users WHERE kioskid = @kioskid AND username = @username"
            Using conn = PostgresConnection.GetConnection()
                conn.Open()
                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)
                    cmd.Parameters.AddWithValue("@username", lstboxLockedUsers.SelectedItem)

                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            userId = reader.GetGuid(0)
                        Else
                            MessageBox.Show("User not found", "User not found", MessageBoxButton.OK, MessageBoxImage.Information)
                            Exit Sub
                        End If
                    End Using
                End Using
            End Using
            GenerateXreport(userId)

            'PrintDocument1.PrinterSettings.Copies = 1
            'PrintDocument1.Print()
            'Dim pq As New PrintQueue(New PrintServer(), "Printer Name")
            'Dim pt As New PrintTicket()
            'pt.CopyCount = 1
            'pq.AddJob("Print Job", "output.xps", False)
            PrintXReportSilent()

            'Unlock User
            Using conn = PostgresConnection.GetConnection()
                conn.Open()
                sql = "UPDATE
                            sessions
                       SET
                            is_active = FALSE,
                            logout_when = CURRENT_TIMESTAMP
                        WHERE
                            kioskid = @kioskid
                        And
                            user_id = @userid
                        And 
                            is_active = TRUE"
                Dim cmd As New NpgsqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@kioskid", kioskid)
                cmd.Parameters.AddWithValue("@userid", userId)
                Dim rowsAffected As Integer = cmd.ExecuteNonQuery()
            End Using
            FillUsers()
        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", sql)
            MessageBox.Show(ex.Message, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error)
        Finally

        End Try
    End Sub

    'Private Sub PrintDocument1_PrintPage(ByVal sender As System.Object, ByVal e As System.Drawing.Printing.PrintPageEventArgs) Handles PrintDocument1.PrintPage
    '    Dim headerFont As Font = New Drawing.Font(REPORT_FONT, 15, FontStyle.Bold)
    '   Dim reportFont As Font = New Drawing.Font(REPORT_FONT, 9)
    '  Dim reportFontSmall As Font = New Drawing.Font(REPORT_FONT, 9)

    ' e.Graphics.DrawString(KIOSK_NAME, headerFont, Brushes.Black, 65, 0)
    ' e.Graphics.DrawString(COMPANY_NAME, reportFont, Brushes.Black, 95, 35)
    ' e.Graphics.DrawString(KIOSK_ADDRESS1, reportFont, Brushes.Black, 75, 50)
    ' e.Graphics.DrawString(KIOSK_ADDRESS2, reportFont, Brushes.Black, 95, 65)
    ' e.Graphics.DrawString(COMPANY_VAT, reportFont, Brushes.Black, 60, 80)

    '        e.Graphics.DrawString(SINGE_DASHED_LINE, reportFont, Brushes.Black, 0, 95)
    '        e.Graphics.DrawString("Χρήστης: " & getUserByUsername(lstboxLockedUsers.Text), reportFont, Brushes.Black, 0, 110)

    '       Dim cmd As New OracleCommand("", conn)
    '      Dim dr As OracleDataReader

    '     Try
    '        cmd = New OracleCommand(GET_TOTALS_FOR_X_REPORT, conn)
    '       Dim userIdparam As New OracleParameter
    '      userIdparam.OracleDbType = OracleDbType.Varchar2
    '     userIdparam.Value = lstBoxUUIDS.Items.Item(lstboxLockedUsers.SelectedIndex)
    '    cmd.Parameters.Add(userIdparam)
    '   dr = cmd.ExecuteReader()
    '  If dr.Read Then
    '     Dim xmargin As Integer = 150

    '    e.Graphics.DrawString("Από: " & CStr(dr(0)), reportFont, Brushes.Black, 0, xmargin)
    '   xmargin += 20
    '  e.Graphics.DrawString("Έως: " & CStr(dr(1)), reportFont, Brushes.Black, 0, xmargin)

    ' Dim totalvat5 As Double = CDbl(dr(3))
    '  Dim totalvat19 As Double = CDbl(dr(4))
    '  Dim payments As Double = CDbl(dr(5))
    '  Dim initial As Double = CDbl(dr(6))
    ' Dim final As Double = CDbl(dr(7))
    '   Dim totalvat0 As Double = CDbl(dr(9))
    'Dim amountlaxeia As Double = CDbl(dr(10))
    '     Dim initialamountlaxeia As Double = CDbl(dr(11))
    '      Dim amountvisa As Double = CDbl(dr(12))
    '       Dim totalvat3 As Double = CDbl(dr(13))
    '
    '               Dim totalreceivedamt As Double = totalvat0 + totalvat3 + totalvat5 + totalvat19
    '                Dim totalamounttodeliver = (totalreceivedamt + initial) - payments - amountvisa

    ' xmargin += 20
    '  e.Graphics.DrawString("αρχικό ποσό: " & initial.ToString("n2"), reportFont, Brushes.Black, 0, xmargin)
    '   xmargin += 20
    '    xmargin += 20
    '     e.Graphics.DrawString("ποσο λαχείων για παράδωση: " & (initialamountlaxeia - amountlaxeia).ToString("n2"), reportFont, Brushes.Black, 0, xmargin)
    '  End If
    '   dr.close()
    'Catch ex As Exception
    '   CreateExceptionFile(ex.Message, " " & get_totals_for_x_report)
    '    MessageBox.Show(ex.Message, "application error", MessageBoxButton.OK, MessageBoxImage.Error)
    ' Finally
    '      cmd.dispose()
    '   End Try
    'End Sub


    Private Sub PrintXReportSilent()

        Dim tempXps As String = System.IO.Path.GetTempFileName() & ".xps"

        ' Create XPS document
        Using xpsDoc As New XpsDocument(tempXps, IO.FileAccess.ReadWrite)

            Dim writer As XpsDocumentWriter = XpsDocument.CreateXpsDocumentWriter(xpsDoc)

            Dim dv As New DrawingVisual()

            Using dc As DrawingContext = dv.RenderOpen()

                Dim headerFont As New Typeface(REPORT_FONT)
                Dim reportFont As New Typeface(REPORT_FONT)

                Dim y As Double = 0

                ' Header
                dc.DrawText(New FormattedText(
                KIOSK_NAME,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                headerFont,
                15,
                Brushes.Black), New Point(65, y))

                y += 35
                dc.DrawText(New FormattedText(COMPANY_NAME, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, reportFont, 9, Brushes.Black), New Point(95, y))

                y += 15
                dc.DrawText(New FormattedText(KIOSK_ADDRESS1, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, reportFont, 9, Brushes.Black), New Point(75, y))

                y += 15
                dc.DrawText(New FormattedText(KIOSK_ADDRESS2, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, reportFont, 9, Brushes.Black), New Point(95, y))

                y += 15
                dc.DrawText(New FormattedText(COMPANY_VAT, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, reportFont, 9, Brushes.Black), New Point(60, y))

                y += 15
                dc.DrawText(New FormattedText(SINGE_DASHED_LINE, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, reportFont, 9, Brushes.Black), New Point(0, y))

                y += 15
                dc.DrawText(New FormattedText(
                "Χρήστης: " + lstboxLockedUsers.SelectedItem,  '& getUserByUsername(lstboxLockedUsers.SelectedItem),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                reportFont,
                9,
                Brushes.Black), New Point(0, y))

                ' ---- DATABASE PART ----
                Dim sql As String =
"SELECT
    from_date,
    to_date,
    total_receipts,
    total5percent,
    total19percent,
    payments,
    initial_amt,
    final_amt,
    description,
    total0percent,
    amount_laxeia,
    initialamtlaxeia,
    amountvisa,
    total3percent
FROM x_report
WHERE kioskid = @kioskid
  AND user_id = @userid
  AND created_on = (
        SELECT MAX(created_on)
        FROM x_report
        WHERE kioskid = @kioskid
          AND user_id = @userid
  );
"

                Using conn = PostgresConnection.GetConnection()
                    conn.Open()

                    Using cmd As New NpgsqlCommand(sql, conn)

                        cmd.Parameters.AddWithValue("@kioskid", kioskid)
                        cmd.Parameters.AddWithValue("@userid", userid)

                        Using dr As NpgsqlDataReader = cmd.ExecuteReader()

                            If dr.Read() Then

                                y += 40
                                dc.DrawText(New FormattedText(
                                    "Από: " & dr.GetValue(0).ToString(),
                                    CultureInfo.CurrentCulture,
                                    FlowDirection.LeftToRight,
                                    reportFont,
                                    9,
                                    Brushes.Black), New Point(0, y))

                                y += 20
                                dc.DrawText(New FormattedText(
                                    "Έως: " & dr.GetValue(1).ToString(),
                                    CultureInfo.CurrentCulture,
                                    FlowDirection.LeftToRight,
                                    reportFont,
                                    9,
                                    Brushes.Black), New Point(0, y))

                                Dim totalvat0 As Double = CDbl(dr.GetValue(9))
                                Dim totalvat3 As Double = CDbl(dr.GetValue(13))
                                Dim totalvat5 As Double = CDbl(dr.GetValue(3))
                                Dim totalvat19 As Double = CDbl(dr.GetValue(4))
                                Dim payments As Double = CDbl(dr.GetValue(5))
                                Dim initial As Double = CDbl(dr.GetValue(6))
                                Dim amountvisa As Double = CDbl(dr.GetValue(12))
                                Dim initialamountlaxeia As Double = CDbl(dr.GetValue(11))
                                Dim amountlaxeia As Double = CDbl(dr.GetValue(10))

                                Dim totalreceived =
                                    totalvat0 + totalvat3 + totalvat5 + totalvat19

                                Dim totalToDeliver =
                                    (totalreceived + initial) - payments - amountvisa

                                y += 20
                                dc.DrawText(New FormattedText(
                                    "αρχικό ποσό: " & initial.ToString("n2"),
                                    CultureInfo.CurrentCulture,
                                    FlowDirection.LeftToRight,
                                    reportFont,
                                    9,
                                    Brushes.Black), New Point(0, y))

                                y += 40
                                dc.DrawText(New FormattedText(
                                    "ποσο λαχείων για παράδωση: " &
                                    (initialamountlaxeia - amountlaxeia).ToString("n2"),
                                    CultureInfo.CurrentCulture,
                                    FlowDirection.LeftToRight,
                                    reportFont,
                                    9,
                                    Brushes.Black), New Point(0, y))
                            End If

                        End Using
                    End Using
                End Using

            End Using

            writer.Write(dv)
        End Using

        Dim server As New PrintServer()
        Dim queue As PrintQueue = LocalPrintServer.GetDefaultPrintQueue()

        queue.AddJob("X Report", tempXps, False)


    End Sub



    'Protected Overrides ReadOnly Property CreateParams() As CreateParams
    ''Disables X button
    'Get
    'Dim param As CreateParams = MyBase.CreateParams
    '       param.ClassStyle = param.ClassStyle Or &H200
    'Return param
    'End Get
    'End Property



End Class
