Imports System.Data
Imports System.Globalization
Imports System.IO
Imports System.Printing
Imports System.Windows.Xps
Imports System.Windows.Xps.Packaging
Imports kiosk_2026.GlobalModule
Imports Npgsql
Imports Npgsql.Replication.PgOutput.Messages
Imports NpgsqlTypes

Module GlobalModule

    Public AdminWin As AdminWindow


    'Public Conn As NpgsqlConnection
    Public username As String
    Public whois As String
    Public currentUser As User
    Public currentUserID As String

    Public dualMonitor As Boolean = False
    Public computerName As String
    Public kioskid As String

    Public divideFactor0 As Double = 1
    Public divideFactor3 As Double = 1
    Public divideFactor5 As Double = 1
    Public divideFactor19 As Double = 1
    Public minBarcode As Integer
    Public startDate As Date

    'Report arguments
    Public REPORT_FONT As String = "Segoe UI"
    Public SINGE_DASHED_LINE As String = "----------------------------------------------------------------"
    Public Structure User
        Public canViewReports As Boolean
        Public canEditProducts As Boolean
        Public canEditProductsFull As Boolean
        Public isAdmin As Boolean
        Public isUnlock As Boolean
    End Structure

    Public LOGIN_TITLE1 As String = ""
    Public LOGIN_TITLE2 As String = ""
    Public KIOSK_NAME As String = ""
    Public COMPANY_NAME As String = ""
    Public KIOSK_ADDRESS1 As String = ""
    Public KIOSK_ADDRESS2 As String = ""
    Public COMPANY_VAT As String = ""


    Public Function GetAmountLaxeia() As Double
        Dim sql As String =
            "SELECT COALESCE(SUM(sell_amt * avail_quantity), 0) " &
            "FROM lottery l " &
            "JOIN barcodes b ON b.barcode = l.barcode " &
            "JOIN products p ON p.serno = b.product_serno " &
            "WHERE p.kioskid='" + kioskid + "';"

        Dim amountLaxeia As Double = 0

        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New NpgsqlCommand(sql, conn)
                    Dim result = cmd.ExecuteScalar()
                    amountLaxeia = If(IsDBNull(result), 0D, CDbl(result))
                End Using
            End Using

        Catch ex As Exception
            CreateExceptionFile(ex.Message, " " & sql)
            MessageBox.Show(ex.Message, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try

        Return amountLaxeia
    End Function

    Public Sub CreateExceptionFile(ByVal exception As String, ByVal sql As String)
        Try
            ' Ensure directory exists
            Dim dir As String = "C:\exceptions"
            If Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If

            ' Safe timestamp-based filename
            Dim fileName As String = Date.Now.ToString("yyyyMMdd_HHmmss_fff") & ".txt"
            Dim filePath As String = Path.Combine(dir, "exception_" & fileName)

            ' Write exception + SQL safely
            Using writer As New StreamWriter(filePath, False)
                writer.WriteLine("Exception:")
                writer.WriteLine(exception)
                writer.WriteLine()
                writer.WriteLine("SQL:")
                writer.WriteLine(sql)
                writer.WriteLine()
                writer.WriteLine("Timestamp: " & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            End Using
        Catch ex As Exception
            ' LAST-RESORT fallback message
            MessageBox.Show("Could not write exception log: " & ex.Message,
                            "Logging Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Public Sub GetMinBarcodeLength()
        Dim sql As String = "SELECT COALESCE(MIN(LENGTH(barcode)), 9999999) FROM barcodes WHERE kioskid='" + kioskid + "';"
        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New NpgsqlCommand(sql, conn)
                    Dim result = cmd.ExecuteScalar()
                    minBarcode = If(IsDBNull(result), 0, Convert.ToInt32(result))
                End Using
            End Using
        Catch ex As Exception
            CreateExceptionFile(ex.Message, sql)
            MessageBox.Show(ex.Message, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub
    Private Structure ReceiptTotals
        Public TotalReceipts As Integer
        Public TotalAmt As Double
        Public TotalVat0 As Double
        Public TotalVat3 As Double
        Public TotalVat5 As Double
        Public TotalVat19 As Double
    End Structure

    Public Function GenerateXreport(ByVal userid As Guid) As Boolean
        Dim WhoAmI As String = "GenerateXreport"
        Try
            Dim totals = GetReceiptTotals(userid)
            Dim totalPayments = GetTotalPayments(userid)
            totals.TotalAmt -= totalPayments

            Dim salesDescription = GetSalesDescription(userid)
            Dim amountLaxeia = GetAmountLaxeia(userid)
            Dim amountVisa = GetAmountVisa(userid)
            Dim finalAmountLaxeia = GetAmountLaxeia()

            InsertXReport(userid, totals, totalPayments, amountLaxeia, amountVisa, finalAmountLaxeia)

            Return True
        Catch ex As Exception
            CreateExceptionFile(WhoAmI + ": " + ex.Message, "")
            MessageBox.Show(ex.Message, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error)
            Return False
        End Try
    End Function

    Private Function GetReceiptTotals(userId As Guid) As ReceiptTotals
        Dim WhoAmI As String = "GetReceiptTotals"
        Dim totals As New ReceiptTotals

        Dim sql As String =
                        "SELECT COUNT(*),
                                COALESCE(SUM(total_amt_with_disc), 0),
                                COALESCE(SUM(total_vat5), 0),
                                COALESCE(SUM(total_vat19), 0),
                                COALESCE(SUM(total_vat0), 0),
                                COALESCE(SUM(total_vat3), 0)
                         FROM receipts
                         WHERE kioskid = @kioskid
                           AND created_by = @userid
                           AND created_on BETWEEN COALESCE((
                                SELECT MAX(login_when)
                                FROM sessions
                                WHERE user_id = @userid
                           ), '1970-01-01'::timestamp)
                           AND CURRENT_TIMESTAMP"

        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid.ToString())
                    cmd.Parameters.Add("@userid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = userId

                    Using dr As NpgsqlDataReader = cmd.ExecuteReader()
                        If dr.Read() Then
                            totals.TotalReceipts = dr.GetInt32(0)
                            totals.TotalAmt = dr.GetDouble(1)
                            totals.TotalVat5 = dr.GetDouble(2)
                            totals.TotalVat19 = dr.GetDouble(3)
                            totals.TotalVat0 = dr.GetDouble(4)
                            totals.TotalVat3 = dr.GetDouble(5)
                        End If
                    End Using
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

        Return totals
    End Function

    Private Function GetTotalPayments(userId As Guid) As Double
        Dim WhoAmI As String = "GetTotalPayments"

        Dim sql As String =
        "SELECT COALESCE(SUM(amount), 0) " &
        "FROM payments " &
        "WHERE kioskid = @kioskid " &
        "AND created_by = @userid " &
        "AND created_on BETWEEN ( " &
        "    SELECT MAX(login_when) " &
        "    FROM sessions " &
        "    WHERE kioskid = @kioskid AND user_id = @userid " &
        ") AND CURRENT_TIMESTAMP"

        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()
                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid.ToString())
                    cmd.Parameters.Add("@userid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = userId
                    Dim result = cmd.ExecuteScalar()
                    Return Convert.ToDouble(result)
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
            Return 0
        End Try
    End Function



    Private Function GetSalesDescription(userId As Guid) As String
        Dim WhoAmI As String = "GetSalesDescription"
        Dim result As String = ""

        Dim sql As String =
        "SELECT rd.product_serno, " &
        "       p.description, " &
        "       COUNT(rd.product_serno) AS item_count, " &
        "       SUM(rd.quantity) AS total_qty " &
        "FROM receipts_det rd " &
        "JOIN products p ON p.serno = rd.product_serno " &
        "JOIN receipts r ON r.serno = rd.receipt_serno " &
        "WHERE rd.kioskid = @kioskid " &
        "AND r.kioskid = @kioskid " &
        "AND r.created_by = @userid " &
        "AND r.created_on BETWEEN ( " &
        "    SELECT MAX(login_when) " &
        "    FROM sessions " &
        "    WHERE user_id = @userid " &
        ") AND CURRENT_TIMESTAMP " &
        "GROUP BY rd.product_serno, p.description " &
        "ORDER BY p.description"

        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()
                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@userid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = userId
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid.ToString())

                    Using dr = cmd.ExecuteReader()
                        While dr.Read()
                            result &= $"{Environment.NewLine}{dr.GetString(1)}: {dr.GetInt64(3)}"
                        End While
                    End Using
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

        Return result
    End Function


    Private Function GetAmountLaxeia(userId As Guid) As Double
        Dim WhoAmI As String = "GetAmountLaxeia"

        Dim sql As String =
        "SELECT COALESCE(SUM(rd.amount), 0) " &
        "FROM receipts_det rd " &
        "JOIN receipts r ON r.serno = rd.receipt_serno " &
        "WHERE rd.kioskid = @kioskid " &
        "AND r.kioskid = @kioskid " &
        "AND rd.vat = 0 " &
        "AND r.created_by = @userid " &
        "AND r.created_on BETWEEN ( " &
        "    SELECT MAX(login_when) " &
        "    FROM sessions " &
        "    WHERE kioskid = @kioskid AND user_id = @userid " &
        ") AND CURRENT_TIMESTAMP"

        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()
                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@userid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = userId
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid.ToString())

                    Dim result = cmd.ExecuteScalar()
                    Return Convert.ToDouble(result)
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
            Return 0
        End Try
    End Function


    Private Function GetAmountVisa(userId As Guid) As Double
        Dim WhoAmI As String = "GetAmountVisa"

        Dim sql As String =
        "SELECT COALESCE(SUM(total_amt_with_disc), 0) " &
        "FROM receipts " &
        "WHERE kioskid = @kioskid " &
        "AND payment_type = 'V' " &
        "AND created_by = @userid " &
        "AND created_on BETWEEN ( " &
        "    SELECT MAX(login_when) " &
        "    FROM sessions " &
        "    WHERE kioskid = @kioskid AND user_id = @userid " &
        ") AND CURRENT_TIMESTAMP"

        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()
                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@userid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = userId
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid.ToString())

                    Dim result = cmd.ExecuteScalar()
                    Return Convert.ToDouble(result)
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
            Return 0
        End Try
    End Function

    Private Sub InsertXReport(userId As Guid,
                          totals As ReceiptTotals,
                          totalPayments As Decimal,
                          amountLaxeia As Decimal,
                          amountVisa As Decimal,
                          finalLaxeia As Decimal)

        Dim WhoAmI As String = "InsertXReport"

        Dim sql As String =
            "INSERT INTO x_report ( " &
            "    user_id, from_date, to_date, total_receipts, total_amt, " &
            "    total0percent, total3percent, total5percent, total19percent, " &
            "    initial_amt, final_amt, payments, created_on, description, " &
            "    amount_laxeia, initialamtlaxeia, amountvisa, finalamtlaxeia, kioskid " &
            ") " &
            "VALUES ( " &
            "    @userid, " &
            "    (SELECT MAX(login_when) FROM sessions WHERE user_id = @userid), " &
            "    CURRENT_TIMESTAMP, " &
            "    @total_receipts, @total_amt, @vat0, @vat3, @vat5, @vat19, " &
            "    (SELECT paramvalue::numeric FROM global_params WHERE paramkey = 'init.fiscal.amt'), " &
            "    @total_amt, " &
            "    @payments, CURRENT_TIMESTAMP, '', " &
            "    @amount_laxeia, " &
            "    (SELECT amountlaxeiaonlogin " &
            "     FROM sessions " &
            "     WHERE user_id = @userid " &
            "     ORDER BY login_when DESC LIMIT 1), " &
            "    @amount_visa, @final_laxeia, @kioskid " &
            ")"

        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()
                Using cmd As New Npgsql.NpgsqlCommand(sql, conn)
                    cmd.Parameters.Add("@userid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = userId
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid.ToString())

                    cmd.Parameters.Add("@total_receipts", NpgsqlTypes.NpgsqlDbType.Integer).Value = totals.TotalReceipts
                    cmd.Parameters.Add("@total_amt", NpgsqlTypes.NpgsqlDbType.Numeric).Value = totals.TotalAmt
                    cmd.Parameters.Add("@vat0", NpgsqlTypes.NpgsqlDbType.Numeric).Value = totals.TotalVat0
                    cmd.Parameters.Add("@vat3", NpgsqlTypes.NpgsqlDbType.Numeric).Value = totals.TotalVat3
                    cmd.Parameters.Add("@vat5", NpgsqlTypes.NpgsqlDbType.Numeric).Value = totals.TotalVat5
                    cmd.Parameters.Add("@vat19", NpgsqlTypes.NpgsqlDbType.Numeric).Value = totals.TotalVat19

                    cmd.Parameters.Add("@payments", NpgsqlTypes.NpgsqlDbType.Numeric).Value = totalPayments
                    cmd.Parameters.Add("@amount_laxeia", NpgsqlTypes.NpgsqlDbType.Numeric).Value = amountLaxeia
                    cmd.Parameters.Add("@amount_visa", NpgsqlTypes.NpgsqlDbType.Numeric).Value = amountVisa
                    cmd.Parameters.Add("@final_laxeia", NpgsqlTypes.NpgsqlDbType.Numeric).Value = finalLaxeia

                    cmd.ExecuteNonQuery()
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


    Public Sub LogoutCurrentUser()
        Dim WhoAmI As String = "LogoutUser"
        Dim sql As String = ""

        Try
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
                cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)
                cmd.Parameters.Add("@userid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(currentUserID)
                Dim rowsAffected As Integer = cmd.ExecuteNonQuery()
            End Using
        Catch ex As Exception
            CreateExceptionFile($"{WhoAmI}: {ex.Message}", sql)
            MessageBox.Show(ex.Message, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try

    End Sub

    Public Sub PrintXReportSilent(userId As Guid, userName As String)

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
                "Χρήστης: " + userName,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                reportFont,
                9,
                Brushes.Black), New Point(0, y))

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

                        cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)
                        cmd.Parameters.AddWithValue("@userid", userId)

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


End Module
