Imports MySql.Data.MySqlClient

Module conexion
    Public ConexionBD As New MySqlConnection(
        "server=localhost;database=cafeteria_mitre;uid=root;pwd=;"
    )
End Module
