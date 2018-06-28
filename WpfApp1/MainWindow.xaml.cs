using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Xml;

namespace WpfApp1
{
    public class UsuarioModel
    {
        public string Usuario {
            get; set;
        }
        public string Password {
            get; set;
        }
        public string Salt {
            get;
            set;
        }
        public static string cifrado(UsuarioModel Usuario) {
            StringBuilder merge = new StringBuilder();
            int i = 0, j = 0, k = 0;
            if (!string.IsNullOrEmpty(Usuario.Salt)) {
                while (i < Usuario.Usuario.Length || j < Usuario.Password.Length) {
                    if (i < Usuario.Usuario.Length) {
                        merge.Append(Usuario.Usuario[i]);
                        i++;
                    }
                    merge.Append(Usuario.Salt[k]);
                    k++;
                    if (k == Usuario.Salt.Length)
                        k = 0;
                    if (j < Usuario.Password.Length) {
                        merge.Append(Usuario.Password[j]);
                        j++;
                    }
                }
            } else {
                while (i < Usuario.Usuario.Length || j < Usuario.Password.Length) {
                    if (i < Usuario.Usuario.Length) {
                        merge.Append(Usuario.Usuario[i]);
                        i++;
                    }
                    if (j < Usuario.Password.Length) {
                        merge.Append(Usuario.Password[j]);
                        j++;
                    }
                }
            }
            byte[] bytes = Encoding.Unicode.GetBytes(merge.ToString());
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash) {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }
    }
    public class ConnStringModel
    {
        public string DataSource {
            get; set;
        }
        public string Catalog {
            get; set;
        }
        public string User {
            get; set;
        }
        public string Passwd {
            get; set;
        }
        public bool IntSecurity {
            get; set;
        }
        public override string ToString() {
            if (!IntSecurity)
                return string.Format("Data Source={0}; Initial Catalog={1}; User id={2}; Password={3};", DataSource, Catalog, User, Passwd);
            else
                return string.Format("Data Source={0}; Initial Catalog={1}; Integrated Security=true;", DataSource, Catalog);
        }
        public string ToStringWithoutPass() {
            if (!IntSecurity)
                return string.Format("Data Source={0}; Initial Catalog={1}; User id={2};", DataSource, Catalog, User);
            else
                return string.Format("Data Source={0}; Initial Catalog={1}; Integrated Security=true;", DataSource, Catalog);
        }
    }
    public class ResultadoItem
    {
        public int Id { get; set; }
        public string SKU { get; set; }
        public string Nombre { get; set; }
        public string Precio { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string K_SETTINGS = "settings.xml";
        const string K_SRV_SETTINGS = "servers.xml";
        enum tipoCarga
        {
            Completo = 0,
            Servidores = 1,
            Usuario = 2
        };
        enum eError
        {
            NO_ERROR = 0,
            NO_SETTINGS = 1,
            NO_SERVERS = 2
        }

        private SqlCommand Cmd;
        private SqlConnection Conn;
        private SqlDataAdapter da;

        public string ConnStringOrigen {
            get; private set;
        }
        public string ConnStringDestino {
            get; private set;
        }

        private ConnStringModel ConnStrModelOrigen {
            get; set;
        }
        private ConnStringModel ConnStrModelDestino {
            get; set;
        }
        private UsuarioModel Usuario;

        private eError error;

        public MainWindow() {
            InitializeComponent();
            Usuario = new UsuarioModel();
            ConnStrModelOrigen = new ConnStringModel();
            ConnStrModelDestino = new ConnStringModel();
            error = LoadSettings();
            if (error.Equals(eError.NO_ERROR)) {
                FrmLogin login = new FrmLogin() {
                    Usuario = Usuario
                };
                bool? resultado = login.ShowDialog();
                if (resultado.HasValue ? !resultado.Value : true) {
                    Close();
                }
                Usuario.Password = login.usrlog.Password;
            } else if (error.Equals(eError.NO_SETTINGS)) {
                Close();
            }
            #region asignación de valores a controles
            txtUsuario.Text = Usuario.Usuario;
            txtSalt.Text = Usuario.Salt;

            txtServOrigen.Text = ConnStrModelOrigen.DataSource;
            txtBDOrigen.Text = ConnStrModelOrigen.Catalog;
            txtUsrOrigen.Text = ConnStrModelOrigen.User;
            chkSeguridadOrigen.IsChecked = ConnStrModelOrigen.IntSecurity;

            txtServDestino.Text = ConnStrModelDestino.DataSource;
            txtBDDestino.Text = ConnStrModelDestino.Catalog;
            txtUsrDestino.Text = ConnStrModelDestino.User;
            chkSeguridadDestino.IsChecked = ConnStrModelDestino.IntSecurity;

            txbConnStrOrigen.Text = ConnStrModelOrigen.ToString();
            txbConnStrDestino.Text = ConnStrModelDestino.ToString();

            TextRange range;
            System.IO.FileStream fStream;
            if (System.IO.File.Exists(@".\media\Ayuda.rtf")) {
                range = new TextRange(rtxtContenido.Document.ContentStart, rtxtContenido.Document.ContentEnd);
                fStream = new System.IO.FileStream(@".\media\Ayuda.rtf", System.IO.FileMode.OpenOrCreate);
                range.Load(fStream, System.Windows.DataFormats.Rtf);
                fStream.Close();

            }

            #endregion
        }

        private eError LoadSettings(tipoCarga carga = tipoCarga.Completo) {
            XmlReader reader;
            try {
                #region User settings
                if (carga.Equals(tipoCarga.Completo) || carga.Equals(tipoCarga.Usuario)) {
                    reader = XmlReader.Create(K_SETTINGS);
                    while (reader.Read()) {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "User") {
                            Usuario.Usuario = reader.GetAttribute("ID").ToLower();
                            while (reader.NodeType != XmlNodeType.EndElement) {
                                reader.Read();
                                if (reader.Name == "PASS") {
                                    while (reader.NodeType != XmlNodeType.EndElement) {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            Usuario.Password = reader.Value;
                                    }
                                    reader.Read();
                                }
                                if (reader.Name == "SALT") {
                                    while (reader.NodeType != XmlNodeType.EndElement) {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            Usuario.Salt = reader.Value;
                                    }
                                    reader.Read();
                                }
                            }
                        }
                    }
                }
                #endregion
                #region servers settings
                if (carga.Equals(tipoCarga.Completo) || carga.Equals(tipoCarga.Servidores)) {
                    reader = XmlReader.Create(K_SRV_SETTINGS);
                    while (reader.Read()) {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "Servidor") {
                            if (reader.GetAttribute("Tipo").Equals("Origen")) {
                                ConnStrModelOrigen.DataSource = reader.GetAttribute("DataSource");
                                ConnStrModelOrigen.Catalog = reader.GetAttribute("Catalog");
                                ConnStrModelOrigen.User = reader.GetAttribute("User");
                                ConnStrModelOrigen.IntSecurity = reader.GetAttribute("IntegratedSecurity") == "True";
                            }
                            if (reader.GetAttribute("Tipo").Equals("Destino")) {
                                ConnStrModelDestino.DataSource = reader.GetAttribute("DataSource");
                                ConnStrModelDestino.Catalog = reader.GetAttribute("Catalog");
                                ConnStrModelDestino.User = reader.GetAttribute("User");
                                ConnStrModelDestino.IntSecurity = reader.GetAttribute("IntegratedSecurity") == "True";
                            }
                        }
                    }
                }
                #endregion
            } catch (FileNotFoundException ex) {
                if (ex.FileName.Contains("settings"))
                    return eError.NO_SETTINGS;
                if (ex.FileName.Contains("servers"))
                    return eError.NO_SERVERS;
            }
            return eError.NO_ERROR;
        }

        private IEnumerable<DataGridRow> GetDataGridRows(DataGrid grid) {
            var itemsSource = grid.ItemsSource as IEnumerable;
            if (null == itemsSource)
                yield return null;
            foreach (var item in itemsSource) {
                var row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (null != row)
                    yield return row;
            }
        }

        private void chkSeguridadOrigen_Checked(object sender, RoutedEventArgs e) {
            txtUsrOrigen.IsEnabled = (sender as CheckBox).IsChecked.HasValue ? (sender as CheckBox).IsChecked.Value : true;
            txtPassOrigen.IsEnabled = (sender as CheckBox).IsChecked.HasValue ? (sender as CheckBox).IsChecked.Value : true;
        }

        private void chkSeguridadDestino_Checked(object sender, RoutedEventArgs e) {
            txtUsrDestino.IsEnabled = (sender as CheckBox).IsChecked.HasValue ? (sender as CheckBox).IsChecked.Value : true;
            txtPassDestino.IsEnabled = (sender as CheckBox).IsChecked.HasValue ? (sender as CheckBox).IsChecked.Value : true;
        }

        private void btnGuardarUsuario_Click(object sender, RoutedEventArgs e) {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            Usuario.Usuario = txtUsuario.Text.ToLower();
            Usuario.Password = !string.IsNullOrEmpty(txtPassword.Password) ? txtPassword.Password : Usuario.Password;
            Usuario.Salt = txtSalt.Text;

            XmlWriter writer = XmlWriter.Create(K_SETTINGS, settings);
            writer.WriteStartDocument();
            writer.WriteComment("This file is generated by the program.");
            writer.WriteStartElement("User");
            writer.WriteAttributeString("ID", Usuario.Usuario);
            writer.WriteElementString("PASS", UsuarioModel.cifrado(Usuario));
            writer.WriteElementString("SALT", Usuario.Salt);
            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Flush();
            writer.Close();

        }

        private void btnGuardarServidores_Click(object sender, RoutedEventArgs e) {
            ConnStrModelOrigen.DataSource = txtServOrigen.Text;
            ConnStrModelOrigen.Catalog = txtBDOrigen.Text;
            if (chkSeguridadOrigen.IsChecked.HasValue ? chkSeguridadOrigen.IsChecked.Value : false) {
                ConnStrModelOrigen.IntSecurity = true;
                ConnStrModelOrigen.User = string.Empty;
                ConnStrModelOrigen.Passwd = string.Empty;
            } else {
                ConnStrModelOrigen.IntSecurity = false;
                ConnStrModelOrigen.User = txtUsrOrigen.Text;
                ConnStrModelOrigen.Passwd = txtPassOrigen.Password;
            }
            txbConnStrOrigen.Text = "Cadena de conexión = " + ConnStrModelOrigen.ToStringWithoutPass();

            ConnStrModelDestino.DataSource = txtServDestino.Text;
            ConnStrModelDestino.Catalog = txtBDDestino.Text;
            if (chkSeguridadDestino.IsChecked.HasValue ? chkSeguridadDestino.IsChecked.Value : false) {
                ConnStrModelDestino.IntSecurity = true;
                ConnStrModelDestino.User = string.Empty;
                ConnStrModelDestino.Passwd = string.Empty;
            } else {
                ConnStrModelDestino.IntSecurity = false;
                ConnStrModelDestino.User = txtUsrDestino.Text;
                ConnStrModelDestino.Passwd = txtPassDestino.Password;
            }
            txbConnStrDestino.Text = "Cadena de conexión = " + ConnStrModelDestino.ToStringWithoutPass();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            XmlWriter writer = XmlWriter.Create(K_SRV_SETTINGS, settings);
            writer.WriteStartDocument();
            writer.WriteComment("This file is generated by the program.");
            writer.WriteStartElement("Servers");
            writer.WriteStartElement("Servidor");
            writer.WriteAttributeString("Tipo", "Origen");
            writer.WriteAttributeString("DataSource", ConnStrModelOrigen.DataSource);
            writer.WriteAttributeString("Catalog", ConnStrModelOrigen.Catalog);
            writer.WriteAttributeString("IntegratedSecurity", ConnStrModelOrigen.IntSecurity.ToString());
            writer.WriteAttributeString("User", ConnStrModelOrigen.IntSecurity ? string.Empty : ConnStrModelOrigen.User);
            writer.WriteEndElement();
            writer.WriteStartElement("Servidor");
            writer.WriteAttributeString("Tipo", "Destino");
            writer.WriteAttributeString("DataSource", ConnStrModelDestino.DataSource);
            writer.WriteAttributeString("Catalog", ConnStrModelDestino.Catalog);
            writer.WriteAttributeString("IntegratedSecurity", ConnStrModelDestino.IntSecurity.ToString());
            writer.WriteAttributeString("User", ConnStrModelDestino.IntSecurity ? string.Empty : ConnStrModelDestino.User);
            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Flush();
            writer.Close();

        }

        private void btnRevertirUsuario_Click(object sender, RoutedEventArgs e) {
            LoadSettings(tipoCarga.Usuario);
        }

        private void btnCancealrServidores_Click(object sender, RoutedEventArgs e) {
            LoadSettings(tipoCarga.Servidores);
        }

        private void btnOrigen_Click(object sender, RoutedEventArgs e) {
            try {
                using (Conn = new SqlConnection(ConnStrModelOrigen.ToString())) {
                    using (Cmd = new SqlCommand() {
                        Connection = Conn,
                        CommandText = @"SELECT  CAST(LTRIM(RTRIM(CIDPRODUCTO)) AS VARCHAR(MAX)) Id, CAST(LTRIM(RTRIM(CNOMBREPRODUCTO)) AS VARCHAR(MAX)) Nombre,
                                                '' Descripcion,
                                                CAST(LTRIM(RTRIM(CCODIGOPRODUCTO)) AS VARCHAR(MAX)) Codigo, CPRECIO1 Precio
                                        FROM    dbo.admProductos
                                        ORDER   BY CAST(LTRIM(RTRIM(CCODIGOPRODUCTO)) AS VARCHAR(MAX))
                                        COLLATE Traditional_Spanish_ci_ai ASC",
                        CommandType = System.Data.CommandType.Text
                    }) {
                        da = new SqlDataAdapter(Cmd);
                        DataSet ds = new DataSet();
                        da.Fill(ds);
                        dgOrigen.ItemsSource = ds.Tables[0].DefaultView;
                        lblTotalOrigen.Content = "Registros en Origen: " + ds.Tables[0].Rows.Count.ToString();

                    }
                }
                btnAnalizar.IsEnabled = (dgOrigen.Items.Count > 0 && dgDestino.Items.Count > 0);
                lblOrigen.Content = "Origen: " + ConnStrModelOrigen.DataSource + "." + ConnStrModelOrigen.Catalog;
            } catch (Exception ex) {
                lblOrigen.Content = "Origen: " + ex.Message;
            } finally {
                lblStatOrigen.Content = string.Empty;
                lblStatQuery.Content = string.Empty;
            }
        }

        private void btnDestino_Click(object sender, RoutedEventArgs e) {
            try {
                using (Conn = new SqlConnection(ConnStrModelDestino.ToString())) {
                    using (Cmd = new SqlCommand() {
                        Connection = Conn,
                        CommandText = @"SELECT  CAST(LTRIM(RTRIM(Id)) AS VARCHAR(MAX)) Id, CAST(LTRIM(RTRIM(Name)) AS VARCHAR(MAX)) Nombre,
                                                CAST(LTRIM(RTRIM(ShortDescription)) AS VARCHAR(MAX)) ShortDescription,
                                                CAST(LTRIM(RTRIM(SKU)) AS VARCHAR(MAX)) SKU, Price AS Precio
                                        FROM    dbo.Product
                                        ORDER   BY CAST(LTRIM(RTRIM(Name)) AS VARCHAR(MAX))
                                        COLLATE Traditional_Spanish_ci_ai ASC",
                        CommandType = System.Data.CommandType.Text
                    }) {
                        da = new SqlDataAdapter(Cmd);
                        DataSet ds = new DataSet();
                        da.Fill(ds);
                        dgDestino.ItemsSource = ds.Tables[0].DefaultView;
                        lblTotalDestino.Content = "Registros en Destino: " + ds.Tables[0].Rows.Count.ToString();
                    }
                }
                btnAnalizar.IsEnabled = (dgOrigen.Items.Count > 0 && dgDestino.Items.Count > 0);
                lblDestino.Content = "Destino: " + ConnStrModelDestino.DataSource + "." + ConnStrModelDestino.Catalog;
            } catch (Exception ex) {
                lblDestino.Content = "Destino: " + ex.Message;
            } finally {
                lblStatDestino.Content = string.Empty;
                lblStatQuery.Content = string.Empty;
            }
        }

        private void btnAnalizar_Click(object sender, RoutedEventArgs e) {
            GridView gvErroresOrigen = new GridView(), gvErroresDestino = new GridView();
            int i = 0, j = 0, k = 0, finOrigen, finDestino;
            StringBuilder sbQuery = new StringBuilder();
            var dvOrigen = (dgOrigen.ItemsSource as DataView).Table;
            var dvDestino = (dgDestino.ItemsSource as DataView).Table;
            List<object> filaOrigen = new List<object>(), filaDestino = new List<object>();
            finOrigen = dvOrigen.Rows.Count;
            finDestino = dvDestino.Rows.Count;

            gvErroresOrigen.Columns.Add(new GridViewColumn() {
                Header = "Id",
                DisplayMemberBinding = new Binding("Id"),
                Width = 50D
            });
            gvErroresOrigen.Columns.Add(new GridViewColumn() {
                Header = "Código",
                DisplayMemberBinding = new Binding("SKU")
            });
            gvErroresOrigen.Columns.Add(new GridViewColumn() {
                Header = "Nombre",
                DisplayMemberBinding = new Binding("Nombre")
            });
            gvErroresOrigen.Columns.Add(new GridViewColumn() {
                Header = "Precio",
                DisplayMemberBinding = new Binding("Precio")
            });

            gvErroresDestino.Columns.Add(new GridViewColumn() {
                Header = "Id",
                DisplayMemberBinding = new Binding("Id"),
                Width = 50D
            });
            gvErroresDestino.Columns.Add(new GridViewColumn() {
                Header = "Código",
                DisplayMemberBinding = new Binding("SKU"),
                Width = 255
            });
            gvErroresDestino.Columns.Add(new GridViewColumn() {
                Header = "Nombre",
                DisplayMemberBinding = new Binding("Nombre"),
                Width = 255
            });
            gvErroresDestino.Columns.Add(new GridViewColumn() {
                Header = "Precio",
                DisplayMemberBinding = new Binding("Precio"),
                Width = 255
            });

            lstErroresOrigen.Items.Clear();
            lstErroresDestino.Items.Clear();
            lstErroresOrigen.View = gvErroresOrigen;
            lstErroresDestino.View = gvErroresDestino;

            while (i <= finOrigen && j <= finDestino) {
                if (i < finOrigen)
                    filaOrigen = (dvOrigen.Rows[i] as DataRow).ItemArray.ToList();
                if (j < finDestino)
                    filaDestino = (dvDestino.Rows[j] as DataRow).ItemArray.ToList();
                if (filaOrigen.Count > 0 && filaDestino.Count > 0) {
                    string IdO = filaOrigen[3].ToString().ToUpperInvariant(), // 3 = [Codigo]
                        IdD = filaDestino[1].ToString().ToUpperInvariant(); // 1 = [Nombre] donde colocan el código
                    int cmp = string.Compare(IdO, IdD, true);
                    if (cmp < 0) {
                        lstErroresOrigen.Items.Add(new ResultadoItem() {
                            Id = int.Parse(filaOrigen[0].ToString()),
                            SKU = filaOrigen[3].ToString().ToUpperInvariant(),
                            Nombre = filaOrigen[1].ToString(),
                            Precio = filaOrigen[4].ToString()
                        });
                        i++;
                    } else if (cmp == 0) {
                        if (chkDistintos.IsChecked.HasValue ? chkDistintos.IsChecked.Value : false) {
                            if (decimal.Parse(filaOrigen[4].ToString()) != decimal.Parse(filaDestino[4].ToString())) {
                                sbQuery.AppendLine(
                                        string.Format("UPDATE dbo.Product SET Price = '{0}' WHERE Id = '{1}'; ", filaOrigen[4].ToString(), filaDestino[0].ToString())
                                        );
                                k++;
                            }
                        } else {
                            sbQuery.AppendLine(
                                    string.Format("UPDATE dbo.Product SET Price = '{0}' WHERE Id = '{1}'; ", filaOrigen[4].ToString(), filaDestino[0].ToString())
                                    );
                            k++;
                        }
                        i++;
                        j++;
                    } else if (cmp > 0) {
                        lstErroresDestino.Items.Add(new ResultadoItem() {
                            Id = int.Parse(filaDestino[0].ToString()),
                            SKU = filaDestino[1].ToString(),
                            Nombre = filaDestino[2].ToString(),
                            Precio = filaDestino[4].ToString()
                        });
                        j++;
                    }
                }
            }

            txtQuery.Text = sbQuery.ToString();
            tabQuery.IsEnabled = (!string.IsNullOrEmpty(txtQuery.Text)) || (lstErroresOrigen.Items.Count > 0);
            btnSincronizar.IsEnabled = !string.IsNullOrEmpty(txtQuery.Text);
            lblStatOrigen.Content = "Registros no encontrados en la tabla destino: " + lstErroresOrigen.Items.Count.ToString();
            lblStatDestino.Content = "Registros no encontrados en la tabla origen: " + lstErroresDestino.Items.Count.ToString();
            lblStatQuery.Content = "Registros empatados por actualizar: " + k.ToString();
        }

        private void btnSincronizar_Click(object sender, RoutedEventArgs e) {
            try {
                using (Conn = new SqlConnection(ConnStrModelDestino.ToString())) {
                    using (Cmd = new SqlCommand() {
                        Connection = Conn,
                        CommandText = txtQuery.Text,
                        CommandType = System.Data.CommandType.Text
                    }) {
                        Conn.Open();
                        Cmd.ExecuteNonQuery();
                        Conn.Close();
                    }
                }
                System.Windows.Forms.MessageBox.Show("Se realizó la operación correctamente. Se procede a verificar los cambios", "Sincronizador", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                btnOrigen_Click(sender, e);
                btnDestino_Click(sender, e);
                btnAnalizar_Click(sender, e);
            } catch (Exception ex) {
                System.Windows.Forms.MessageBox.Show("Ocurrió el siguiente error al intentar sincronizar: " + ex.Message, "Sincronizador", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void ExportarDestino_Click(object sender, RoutedEventArgs e) {
            Microsoft.Office.Interop.Excel.Application app = new Microsoft.Office.Interop.Excel.Application();
            app.Visible = true;
            Microsoft.Office.Interop.Excel.Workbook wb = app.Workbooks.Add(1);
            Microsoft.Office.Interop.Excel.Worksheet ws = (Microsoft.Office.Interop.Excel.Worksheet)wb.Worksheets[1];
            int i = 1;
            for (i = 0; i < lstErroresDestino.Items.Count; i++) {
                ResultadoItem lvi = (lstErroresDestino.Items[i] as ResultadoItem);
                ws.Cells[i + 1, 2] = lvi.Id.ToString();
                ws.Cells[i + 1, 2] = lvi.Nombre;
                ws.Cells[i + 1, 3] = lvi.SKU;
                ws.Cells[i + 1, 4] = lvi.Precio;
            }
        }

        private void CopiarDestino_Click(object sender, RoutedEventArgs e) {
            StringBuilder sb = new StringBuilder();
            foreach (ResultadoItem item in lstErroresDestino.Items) {
                sb.Append(item.Id.ToString() + "\t");
                sb.Append(item.Nombre.ToString() + "\t");
                sb.Append(item.SKU.ToString() + "\t");
                sb.Append(item.Precio.ToString() + "\n");
            }
            Clipboard.SetText(sb.ToString());
        }

        private void ExportarOrigen_Click(object sender, RoutedEventArgs e) {
            try {
                Microsoft.Office.Interop.Excel.Application app = new Microsoft.Office.Interop.Excel.Application();
                app.Visible = true;
                Microsoft.Office.Interop.Excel.Workbook wb = app.Workbooks.Add(1);
                Microsoft.Office.Interop.Excel.Worksheet ws = (Microsoft.Office.Interop.Excel.Worksheet)wb.Worksheets[1];
                int i = 1;
                for (i = 0; i < lstErroresOrigen.Items.Count; i++) {
                    ResultadoItem lvi = (lstErroresOrigen.Items[i] as ResultadoItem);
                    ws.Cells[i + 1, 2] = lvi.Id.ToString();
                    ws.Cells[i + 1, 2] = lvi.Nombre;
                    ws.Cells[i + 1, 3] = lvi.SKU;
                    ws.Cells[i + 1, 4] = lvi.Precio;
                }
            }catch (Exception ex) {

            }
        }

        private void CopiarOrigen_Click(object sender, RoutedEventArgs e) {
            StringBuilder sb = new StringBuilder();
            try {
            foreach (ResultadoItem item in lstErroresOrigen.Items) {
                sb.Append(item.Id.ToString() + "\t");
                sb.Append(item.Nombre.ToString() + "\t");
                sb.Append(item.SKU.ToString() + "\t");
                sb.Append(item.Precio.ToString() + "\n");
            }
            } catch (Exception ex) {

            } finally {
                Clipboard.SetText(sb.ToString());
            }
        }

        private void btnOrigenExist_Click(object sender, RoutedEventArgs e) {
            try {
                using (Conn = new SqlConnection(ConnStrModelOrigen.ToString())) {
                    using (Cmd = new SqlCommand() {
                        Connection = Conn,
                        CommandText = @"
select  CAST(LTRIM(RTRIM(P.CIDPRODUCTO)) AS VARCHAR(MAX)) IdProducto,
		CAST(LTRIM(RTRIM(P.CNOMBREPRODUCTO)) AS VARCHAR(MAX)) Nombre,
		CAST(LTRIM(RTRIM(P.CCODIGOPRODUCTO)) AS VARCHAR(MAX)) Codigo,
		ISNULL(EC.cEntradasPeriodo1 + EC.cEntradasPeriodo2 +
        EC.cEntradasPeriodo3 + EC.cEntradasPeriodo4 +
		EC.cEntradasPeriodo5 + EC.cEntradasPeriodo6 +
        EC.cEntradasPeriodo7 + EC.cEntradasPeriodo8 +
		EC.cEntradasPeriodo9 + EC.cEntradasPeriodo10 +
        EC.cEntradasPeriodo11 + EC.cEntradasPeriodo12, 0) -
		ISNULL(EC.cSalidasPeriodo1 + EC.cSalidasPeriodo2 +
        EC.cSalidasPeriodo3 + EC.cSalidasPeriodo4 +
		EC.cSalidasPeriodo5 + EC.cSalidasPeriodo6 +
        EC.cSalidasPeriodo7 + EC.cSalidasPeriodo8 +
		EC.cSalidasPeriodo9 + EC.cSalidasPeriodo10 +
        EC.cSalidasPeriodo11 + EC.cSalidasPeriodo12, 0) Existencias
from    admExistenciaCosto AS EC full join admProductos AS P
ON      EC.cIdProducto = P.cIdProducto full join admAlmacenes AS A
ON      EC.cIdAlmacen = A.cIdAlmacen full join admEjercicios AS E
ON      EC.cIdEjercicio = E.cIdEjercicio
WHERE	A.CCODIGOALMACEN = 'AMDD' AND E.CEJERCICIO = DATEPART(YEAR, GETDATE())
ORDER   BY CAST(LTRIM(RTRIM(P.CCODIGOPRODUCTO)) AS VARCHAR(MAX))
		COLLATE Traditional_Spanish_ci_ai ASC",
                        CommandType = System.Data.CommandType.Text
                    }) {
                        da = new SqlDataAdapter(Cmd);
                        DataSet ds = new DataSet();
                        da.Fill(ds);
                        dgOrigenExist.ItemsSource = ds.Tables[0].DefaultView;
                        lblTotalOrigenExist.Content = "Registros en Origen: " + ds.Tables[0].Rows.Count.ToString();

                    }
                }
                btnAnalizarExist.IsEnabled = (dgOrigenExist.Items.Count > 0 && dgDestinoExist.Items.Count > 0);
                lblOrigenExist.Content = "Origen: " + ConnStrModelOrigen.DataSource + "." + ConnStrModelOrigen.Catalog;
            } catch (Exception ex) {
                lblOrigenExist.Content = "Origen: " + ex.Message;
            } finally {
                lblStatOrigen.Content = string.Empty;
                lblStatQuery.Content = string.Empty;
            }

        }

        private void btnDestinoExist_Click(object sender, RoutedEventArgs e) {
            try {
                using (Conn = new SqlConnection(ConnStrModelDestino.ToString())) {
                    using (Cmd = new SqlCommand() {
                        Connection = Conn,
                        CommandText = @"
SELECT  CAST(LTRIM(RTRIM(Id)) AS VARCHAR(MAX)) Id, CAST(LTRIM(RTRIM(Name)) AS VARCHAR(MAX)) Nombre,
		CAST(LTRIM(RTRIM(ShortDescription)) AS VARCHAR(MAX)) ShortDescription,
		CAST(LTRIM(RTRIM(SKU)) AS VARCHAR(MAX)) SKU, StockQuantity Existencias
FROM    dbo.Product
ORDER   BY CAST(LTRIM(RTRIM(Name)) AS VARCHAR(MAX))
		COLLATE Traditional_Spanish_ci_ai ASC
",
                        CommandType = System.Data.CommandType.Text
                    }) {
                        da = new SqlDataAdapter(Cmd);
                        DataSet ds = new DataSet();
                        da.Fill(ds);
                        dgDestinoExist.ItemsSource = ds.Tables[0].DefaultView;
                        lblTotalDestinoExist.Content = "Registros en Destino: " + ds.Tables[0].Rows.Count.ToString();
                    }
                }
                btnAnalizarExist.IsEnabled = (dgOrigenExist.Items.Count > 0 && dgDestinoExist.Items.Count > 0);
                lblDestinoExist.Content = "Destino: " + ConnStrModelDestino.DataSource + "." + ConnStrModelDestino.Catalog;
            } catch (Exception ex) {
                lblDestinoExist.Content = "Destino: " + ex.Message;
            } finally {
                lblStatDestino.Content = string.Empty;
                lblStatQuery.Content = string.Empty;
            }
        }

        private void btnAnalizarExist_Click(object sender, RoutedEventArgs e) {
            GridView gvErroresOrigen = new GridView(), gvErroresDestino = new GridView();
            int i = 0, j = 0, k = 0, finOrigen, finDestino;
            StringBuilder sbQuery = new StringBuilder();
            var dvOrigen = (dgOrigenExist.ItemsSource as DataView).Table;
            var dvDestino = (dgDestinoExist.ItemsSource as DataView).Table;
            List<object> filaOrigen = new List<object>(), filaDestino = new List<object>();
            finOrigen = dvOrigen.Rows.Count;
            finDestino = dvDestino.Rows.Count;

            gvErroresOrigen.Columns.Add(new GridViewColumn() {
                Header = "Id",
                DisplayMemberBinding = new Binding("Id"),
                Width = 50D
            });
            gvErroresOrigen.Columns.Add(new GridViewColumn() {
                Header = "Código",
                DisplayMemberBinding = new Binding("SKU")
            });
            gvErroresOrigen.Columns.Add(new GridViewColumn() {
                Header = "Nombre",
                DisplayMemberBinding = new Binding("Nombre")
            });
            gvErroresOrigen.Columns.Add(new GridViewColumn() {
                Header = "Existencias",
                DisplayMemberBinding = new Binding("Precio")
            });

            gvErroresDestino.Columns.Add(new GridViewColumn() {
                Header = "Id",
                DisplayMemberBinding = new Binding("Id"),
                Width = 50D
            });
            gvErroresDestino.Columns.Add(new GridViewColumn() {
                Header = "Código",
                DisplayMemberBinding = new Binding("SKU"),
                Width = 255
            });
            gvErroresDestino.Columns.Add(new GridViewColumn() {
                Header = "Nombre",
                DisplayMemberBinding = new Binding("Nombre"),
                Width = 255
            });
            gvErroresDestino.Columns.Add(new GridViewColumn() {
                Header = "Existencias",
                DisplayMemberBinding = new Binding("Precio"),
                Width = 255
            });

            lstErroresOrigen.Items.Clear();
            lstErroresDestino.Items.Clear();
            lstErroresOrigen.View = gvErroresOrigen;
            lstErroresDestino.View = gvErroresDestino;

            while (i <= finOrigen && j <= finDestino) {
                if (i < finOrigen)
                    filaOrigen = (dvOrigen.Rows[i] as DataRow).ItemArray.ToList();
                if (j < finDestino)
                    filaDestino = (dvDestino.Rows[j] as DataRow).ItemArray.ToList();
                if (filaOrigen.Count > 0 && filaDestino.Count > 0) {
                    string IdO = filaOrigen[2].ToString().ToUpperInvariant(), // 3 = [Codigo]
                        IdD = filaDestino[1].ToString().ToUpperInvariant(); // 1 = [Nombre] donde colocan el código
                    int cmp = string.Compare(IdO, IdD, true);
                    if (cmp < 0) {
                        lstErroresOrigen.Items.Add(new ResultadoItem() {
                            Id = int.Parse(filaOrigen[0].ToString()),
                            SKU = filaOrigen[2].ToString().ToUpperInvariant(),
                            Nombre = filaOrigen[1].ToString(),
                            Precio = filaOrigen[3].ToString()
                        });
                        i++;
                    } else if (cmp == 0) {
                        if (chkDistintosExist.IsChecked.HasValue ? chkDistintosExist.IsChecked.Value : false) {
                            if (decimal.Parse(filaOrigen[3].ToString()) != decimal.Parse(filaDestino[4].ToString())) {
                                sbQuery.AppendLine(
                                        string.Format("UPDATE dbo.Product SET StockQuantity = '{0}' WHERE Id = '{1}'; ", filaOrigen[3].ToString(), filaDestino[0].ToString())
                                        );
                                k++;
                            }
                        } else {
                            sbQuery.AppendLine(
                                    string.Format("UPDATE dbo.Product SET StockQuantity = '{0}' WHERE Id = '{1}'; ", filaOrigen[3].ToString(), filaDestino[0].ToString())
                                    );
                            k++;
                        }
                        i++;
                        j++;
                    } else if (cmp > 0) {
                        lstErroresDestino.Items.Add(new ResultadoItem() {
                            Id = int.Parse(filaDestino[0].ToString()),
                            SKU = filaDestino[1].ToString(),
                            Nombre = filaDestino[2].ToString(),
                            Precio = filaDestino[4].ToString()
                        });
                        j++;
                    }
                }
            }

            txtQuery.Text = sbQuery.ToString();
            tabQuery.IsEnabled = (!string.IsNullOrEmpty(txtQuery.Text)) || (lstErroresOrigen.Items.Count > 0);
            btnSincronizarExist.IsEnabled = !string.IsNullOrEmpty(txtQuery.Text);
            lblStatOrigen.Content = "Registros no encontrados en la tabla destino: " + lstErroresOrigen.Items.Count.ToString();
            lblStatDestino.Content = "Registros no encontrados en la tabla origen: " + lstErroresDestino.Items.Count.ToString();
            lblStatQuery.Content = "Registros empatados por actualizar: " + k.ToString();

        }

        private void btnSincronizarExist_Click(object sender, RoutedEventArgs e) {
            try {
                using (Conn = new SqlConnection(ConnStrModelDestino.ToString())) {
                    using (Cmd = new SqlCommand() {
                        Connection = Conn,
                        CommandText = txtQuery.Text,
                        CommandType = System.Data.CommandType.Text
                    }) {
                        Conn.Open();
                        Cmd.ExecuteNonQuery();
                        Conn.Close();
                    }
                }
                System.Windows.Forms.MessageBox.Show("Se realizó la operación correctamente. Se procede a verificar los cambios", "Sincronizador", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                btnOrigen_Click(sender, e);
                btnDestino_Click(sender, e);
                btnAnalizar_Click(sender, e);
            } catch (Exception ex) {
                System.Windows.Forms.MessageBox.Show("Ocurrió el siguiente error al intentar sincronizar: " + ex.Message, "Sincronizador", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }
    }
}
