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
                        CommandText = "SELECT CIDPRODUCTO as Id, CNOMBREPRODUCTO as Nombre, '' AS Descripcion, CCODIGOPRODUCTO as Codigo, CPRECIO1 as Precio FROM dbo.admProductos order by Id",
                        CommandType = System.Data.CommandType.Text
                    }) {
                        da = new SqlDataAdapter(Cmd);
                        DataSet ds = new DataSet();
                        da.Fill(ds);
                        dgOrigen.ItemsSource = ds.Tables[0].DefaultView;
                    }
                }
                btnAnalizar.IsEnabled = (dgOrigen.Items.Count > 0 && dgDestino.Items.Count > 0);
                lblOrigen.Content = "Origen: " + ConnStrModelOrigen.DataSource + "." + ConnStrModelOrigen.Catalog;
            } catch (Exception ex) {
                lblOrigen.Content = "Origen: " + ex.Message;
            }
        }

        private void btnDestino_Click(object sender, RoutedEventArgs e) {
            try {
                using (Conn = new SqlConnection(ConnStrModelDestino.ToString())) {
                    using (Cmd = new SqlCommand() {
                        Connection = Conn,
                        CommandText = "select Id, Name AS Nombre, ShortDescription, SKU, Price AS Precio from dbo.Product order by Id",
                        CommandType = System.Data.CommandType.Text
                    }) {
                        da = new SqlDataAdapter(Cmd);
                        DataSet ds = new DataSet();
                        da.Fill(ds);
                        dgDestino.ItemsSource = ds.Tables[0].DefaultView;
                    }
                }
                btnAnalizar.IsEnabled = (dgOrigen.Items.Count > 0 && dgDestino.Items.Count > 0);
                lblDestino.Content = "Destino: " + ConnStrModelDestino.DataSource + "." + ConnStrModelDestino.Catalog;
            } catch (Exception ex) {
                lblDestino.Content = "Destino: " + ex.Message;
            }
        }

        private void btnAnalizar_Click(object sender, RoutedEventArgs e) {
            GridView gvErroresOrigen = new GridView(), gvErroresDestino = new GridView();
            int i = 0, j = 0, finOrigen, finDestino;
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
                DisplayMemberBinding = new Binding("Codigo")
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
                DisplayMemberBinding = new Binding("Nombre")
            });
            gvErroresDestino.Columns.Add(new GridViewColumn() {
                Header = "Nombre",
                DisplayMemberBinding = new Binding("ShortDescription")
            });
            gvErroresDestino.Columns.Add(new GridViewColumn() {
                Header = "Precio",
                DisplayMemberBinding = new Binding("Precio")
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
                    string IdO = filaOrigen[3].ToString(),
                        IdD = filaDestino[1].ToString();
                    int cmp = IdO.CompareTo(IdD);
                    if (cmp < 0) {
                        i++;
                        lstErroresOrigen.Items.Add(new ResultadoItem() {
                            Id = int.Parse(filaOrigen[0].ToString()),
                            SKU = filaOrigen[3].ToString(),
                            Nombre = filaOrigen[1].ToString(),
                            Precio = filaOrigen[4].ToString()
                        });
                    }
                    if (cmp == 0) {
                        if (decimal.Parse(filaOrigen[4].ToString()) != decimal.Parse(filaDestino[4].ToString()))
                            sbQuery.AppendLine(
                                    string.Format("UPDATE dbo.Product SET Price = '{0}' WHERE Id = '{1}'; ", filaOrigen[4].ToString(), filaDestino[0].ToString())
                                    );
                        i++;
                        j++;
                    }
                    if (cmp > 0) {
                        j++;
                        lstErroresDestino.Items.Add(new ResultadoItem() {
                            Id = int.Parse(filaDestino[0].ToString()),
                            SKU = filaDestino[3].ToString(),
                            Nombre = filaDestino[1].ToString(),
                            Precio = filaDestino[4].ToString()
                        });
                    }
                }
            }

            //lstErroresOrigen.Items.Clear();
            //lstErroresOrigen.View = gvErroresOrigen;

            //bool EN_ORIGEN = false;

            //for (i = 0; i < finOrigen; i++) {
            //    filaOrigen = (dvOrigen.Rows[i] as DataRow).ItemArray.ToList();
            //    for (j = 0; j < finDestino; j++) {
            //        filaDestino = (dvDestino.Rows[j] as DataRow).ItemArray.ToList();
            //        if (filaOrigen.Count > 0 && filaDestino.Count > 0) {
            //            string IdO = filaOrigen[3].ToString().Trim(),
            //                IdD = filaDestino[1].ToString().Trim();
            //            if (IdO == IdD) {
            //                if (decimal.Parse(filaOrigen[4].ToString()) != decimal.Parse(filaDestino[4].ToString()))
            //                    sbQuery.AppendLine(
            //                            string.Format("UPDATE dbo.Product SET Price = '{0}' WHERE Id = '{1}'; ", filaOrigen[4].ToString(), filaDestino[0].ToString())
            //                            );
            //                EN_ORIGEN = true;
            //                break;
            //            }
            //        }
            //    }
            //    if (!EN_ORIGEN) {
            //        lstErroresOrigen.Items.Add(new ResultadoItem() {
            //            Id = int.Parse(filaOrigen[0].ToString()),
            //            SKU = filaOrigen[3].ToString(),
            //            Nombre = filaOrigen[1].ToString(),
            //            Precio = filaOrigen[4].ToString()
            //        });
            //    }
            //    EN_ORIGEN = false;
            //}
            txtQuery.Text = sbQuery.ToString();
            tabQuery.IsEnabled = (!string.IsNullOrEmpty(txtQuery.Text)) || (lstErroresOrigen.Items.Count > 0);
            btnSincronizar.IsEnabled = !string.IsNullOrEmpty(txtQuery.Text);
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

    }
}
