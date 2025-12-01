Imports System.Data
Imports System.IO
Imports Npgsql

Module GlobalModule

    'Public Conn As NpgsqlConnection
    Public username As String
    Public whois As String
    Public currentUser As User
    Public dualMonitor As Boolean = False
    Public computerName As String
    Public divideFactor0 As Double = 1
    Public divideFactor3 As Double = 1
    Public divideFactor5 As Double = 1
    Public divideFactor19 As Double = 1
    Public minBarcode As Integer
    Public startDate As Date

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


    Public Function getAmountLaxeia() As Double
        Dim sql As String =
            "SELECT COALESCE(SUM(sell_amt * avail_quantity), 0) " &
            "FROM lottery l " &
            "JOIN barcodes b ON b.barcode = l.barcode " &
            "JOIN products p ON p.serno = b.product_serno;"

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

    Public Sub getMinBarcodeLength()
        Dim sql As String = "SELECT COALESCE(MIN(LENGTH(barcode)), 9999999) FROM barcodes;"
        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New NpgsqlCommand(sql, conn)
                    Dim result = cmd.ExecuteScalar()
                    minBarcode = If(IsDBNull(result), 0, Convert.ToInt32(result))
                End Using
            End Using
        Catch ex As Exception
            createExceptionFile(ex.Message, sql)
            MessageBox.Show(ex.Message, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


End Module
