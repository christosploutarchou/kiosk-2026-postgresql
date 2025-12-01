Imports System.Data
Imports Npgsql

Public Class PostgresConnection

    Private Shared ReadOnly _connString As String =
        "Host=localhost;Port=5432;Username=postgres;Password=christos;Database=kiosk"

    Public Shared Function GetConnection() As NpgsqlConnection
        Return New NpgsqlConnection(_connString)
    End Function

End Class
