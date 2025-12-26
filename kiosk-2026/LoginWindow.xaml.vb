Imports System.Data
Imports System.IO
Imports Npgsql

Class LoginWindow

    Private Sub LoginWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '--------------------------------------------------
        ' 1- Load params file (params.txt)
        '--------------------------------------------------
        Dim FILE_NAME As String = "C:\params.txt"
        Dim sql As String = ""

        If Not File.Exists(FILE_NAME) Then
            MessageBox.Show("Error while reading params file. Contact admin.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            computerName = "-1"
            Return
        End If

        Try
            Dim content As String = File.ReadAllText(FILE_NAME)
            Dim params As String() = content.Split("~"c)

            If params.Length < 6 Then
                MessageBox.Show("File could not be processed.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            computerName = params(0)
            divideFactor0 = params(1).Replace(",", ".")
            divideFactor5 = params(2).Replace(",", ".")
            divideFactor19 = params(3).Replace(",", ".")
            dualMonitor = (params(4).Trim() = "1")
            kioskid = params(5)

        Catch ex As Exception
            MessageBox.Show("Error loading parameters: " & ex.Message)
            Return
        End Try

        '--------------------------------------------------
        ' 1 - Load users from PostgreSQL
        '--------------------------------------------------
        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                ' Use parameterized query to prevent SQL injection
                sql = "SELECT username FROM users WHERE kioskid = @kioskid ORDER BY username;"
                Using cmd As New NpgsqlCommand(sql, conn)
                    ' Ensure kioskid is passed as UUID type if needed
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)

                    Using reader = cmd.ExecuteReader()
                        lstboxUsers.Items.Clear()
                        While reader.Read()
                            lstboxUsers.Items.Add(reader("username").ToString())
                        End While
                    End Using
                End Using

                Console.WriteLine("Connected to DB: " & conn.Database)
            End Using
        Catch ex As Exception
            ' Save exception details and the query for debugging
            CreateExceptionFile(ex.Message, "SELECT username FROM users WHERE kioskid= '" & kioskid & "' ORDER BY username;")
            MessageBox.Show("Error loading users: " & ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error)
            Return
        End Try

        '--------------------------------------------------
        ' 2 - Load global params / min barcode
        '--------------------------------------------------
        Try
            ' Single query using UNION ALL
            sql = "
                    SELECT paramkey, paramvalue
                    FROM global_params
                    WHERE kioskid = @kioskid 
                      AND paramkey IN (
                          'start.date',
                          'login.title1', 'login.title2', 'kiosk.name', 'company.name',
                          'kiosk.address1', 'kiosk.address2', 'company.vat'
                      )
                    UNION ALL
                    SELECT 'min.barcode.length' AS paramkey, COALESCE(MIN(LENGTH(barcode))::text, '0') AS paramvalue
                    FROM barcodes
                    WHERE kioskid = @kioskid;
                "

            Dim appParams As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New NpgsqlCommand(sql, conn)
                    ' Pass kioskid as UUID
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)

                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim key As String = reader("paramkey").ToString()
                            Dim value As String = reader("paramvalue").ToString()
                            appParams(key) = value
                        End While
                    End Using
                End Using
            End Using

            ' Map dictionary values to variables
            If appParams.ContainsKey("start.date") Then
                startDate = DateTime.ParseExact(appParams("start.date"), "dd/MM/yy", Globalization.CultureInfo.InvariantCulture)
            End If

            LOGIN_TITLE1 = If(appParams.ContainsKey("login.title1"), appParams("login.title1"), "")
            LOGIN_TITLE2 = If(appParams.ContainsKey("login.title2"), appParams("login.title2"), "")
            KIOSK_NAME = If(appParams.ContainsKey("kiosk.name"), appParams("kiosk.name"), "")
            COMPANY_NAME = If(appParams.ContainsKey("company.name"), appParams("company.name"), "")
            KIOSK_ADDRESS1 = If(appParams.ContainsKey("kiosk.address1"), appParams("kiosk.address1"), "")
            KIOSK_ADDRESS2 = If(appParams.ContainsKey("kiosk.address2"), appParams("kiosk.address2"), "")
            COMPANY_VAT = If(appParams.ContainsKey("company.vat"), appParams("company.vat"), "")
            minBarcode = If(appParams.ContainsKey("min.barcode.length"), Convert.ToInt32(appParams("min.barcode.length")), 0)

            lblLoginTitle1.Content = LOGIN_TITLE1
            lblLoginTitle2.Content = LOGIN_TITLE2

        Catch ex As Exception
            CreateExceptionFile(ex.Message, sql)
            MessageBox.Show(ex.Message, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
        'isPasswordEncrypted()
    End Sub

    Private Sub LstboxUsers_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles lstboxUsers.SelectionChanged
        txtBoxPassword.Clear()
    End Sub

    Private Sub NumberButton_Click(sender As Object, e As RoutedEventArgs)
        Dim btn = CType(sender, Button)
        Dim number As String = btn.Content.ToString()
        txtBoxPassword.Password &= number
    End Sub

    Private Sub BtnClear_Click(sender As Object, e As RoutedEventArgs) Handles btnClear.Click
        txtBoxPassword.Clear()
    End Sub

    Private Sub BtnBack_Click(sender As Object, e As RoutedEventArgs) Handles btnBack.Click
        If txtBoxPassword.Password.Length > 0 Then
            txtBoxPassword.Password = txtBoxPassword.Password.Substring(0, txtBoxPassword.Password.Length - 1)
        End If
    End Sub

    Private Sub BtnExit_Click(sender As Object, e As RoutedEventArgs) Handles btnExit.Click
        Me.Close()
    End Sub

    Private Sub BtnLogin_Click(sender As Object, e As RoutedEventArgs) Handles btnLogin.Click

        username = CStr(lstboxUsers.SelectedValue)
        whois = ""
        Dim isConnected As Boolean = False

        '--------------------------------------------------
        ' 1️ - Check if user already has an active session
        '--------------------------------------------------
        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New NpgsqlCommand("
                SELECT EXISTS (
                    SELECT 1
                    FROM sessions s
                    JOIN users u ON u.uuid = s.user_id
                    WHERE s.kioskid = @kioskid AND u.username = @username
                      AND s.is_active = TRUE
                );
            ", conn)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)
                    cmd.Parameters.AddWithValue("@username", username)
                    isConnected = CBool(cmd.ExecuteScalar())
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show(ex.ToString)
            Return
        End Try

        If isConnected Then
            MessageBox.Show("User already logged in!", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            Return
        End If

        '--------------------------------------------------
        ' 2 - Get user information (permissions, uuid, unlock)
        '--------------------------------------------------
        Dim userUuid As Guid = Guid.Empty

        Try
            Using conn = PostgresConnection.GetConnection()
                conn.Open()

                Using cmd As New NpgsqlCommand("
                SELECT
                    uuid,
                    is_admin,
                    COALESCE(can_view_reports, FALSE) AS can_view_reports,
                    COALESCE(can_edit_products, FALSE) AS can_edit_products,
                    COALESCE(can_edit_products_full, FALSE) AS can_edit_products_full,
                    is_unlock
                FROM users
                WHERE kioskid= @kioskid AND username = @username
                  AND pass = @pass
            ", conn)

                    cmd.Parameters.AddWithValue("@username", username)
                    cmd.Parameters.AddWithValue("@pass", txtBoxPassword.Password)
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)

                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            userUuid = reader.GetGuid(reader.GetOrdinal("uuid"))
                            currentUserID = userUuid.ToString
                            currentUser.isAdmin = reader.GetBoolean(reader.GetOrdinal("is_admin"))
                            currentUser.canViewReports = reader.GetBoolean(reader.GetOrdinal("can_view_reports"))
                            currentUser.canEditProducts = reader.GetBoolean(reader.GetOrdinal("can_edit_products"))
                            currentUser.canEditProductsFull = reader.GetBoolean(reader.GetOrdinal("can_edit_products_full"))
                            currentUser.isUnlock = reader.GetBoolean(reader.GetOrdinal("is_unlock"))
                        Else
                            MessageBox.Show("Invalid username or password", "Login Failed",
                                        MessageBoxButton.OK, MessageBoxImage.Error)
                            Return
                        End If
                    End Using
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show("Login error: " & ex.Message)
            Return
        End Try


        '--------------------------------------------------
        ' 3 - Logic for different user modes
        '--------------------------------------------------
        If currentUser.isUnlock Then
            Dim unlockWindow As New UnlockWindow
            unlockWindow.Show()
            Return
        Else
            Dim amountLaxeia As Double = GetAmountLaxeia()

            Try
                Using conn = PostgresConnection.GetConnection()
                    conn.Open()

                    Using cmd As New NpgsqlCommand("
                        INSERT INTO sessions (
                            uuid,
                            login_when,
                            is_active,
                            machine_name,
                            user_id,
                            amountLaxeiaOnLogin,
                            kioskid
                        )
                        VALUES (
                            gen_random_uuid(),
                            CURRENT_TIMESTAMP,
                            TRUE,
                            @machine,
                            (SELECT uuid FROM users WHERE username = @username),
                            @amount,
                            @kioskid
                        );
                    ", conn)

                        cmd.Parameters.AddWithValue("@machine", computerName)
                        cmd.Parameters.AddWithValue("@username", username)
                        cmd.Parameters.AddWithValue("@amount", amountLaxeia)
                        cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)
                        cmd.ExecuteNonQuery()
                    End Using
                End Using

            Catch ex As Exception
                CreateExceptionFile(ex.Message, "INSERT INTO sessions ...")
                MessageBox.Show("Session insert error: " & ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try

        End If

        If currentUser.isAdmin Then
            Dim adminWindow As New AdminWindow()
            Application.Current.MainWindow.Hide()
            adminWindow.Show()
        Else
            'Dim w As New PosWindow()
            'w.Show()
        End If

        If dualMonitor Then
            'Dim dual As New DualMonitorWindow()
            'dual.Show()
        End If






    End Sub
End Class
