Imports Npgsql
Imports System.Printing
Imports System.Windows.Xps
Imports System.Windows.Xps.Packaging
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Windows.Interop

Class UnlockWindow
    Dim userId As Guid
    Private Const GWL_STYLE As Integer = -16
    Private Const WS_SYSMENU As Integer = &H80000

    Private Sub UnlockWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Application.Current.MainWindow.Hide()
        FillUsers()
    End Sub

    Private Sub UnlockWindow_Closed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Closed
        Application.Current.MainWindow.Show()
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
                    cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)

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
            PrintXReportSilent(userId, username)

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
                cmd.Parameters.Add("@kioskid", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(kioskid)
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
