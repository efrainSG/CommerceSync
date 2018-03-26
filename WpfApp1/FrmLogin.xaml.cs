using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApp1 {
    /// <summary>
    /// Interaction logic for FrmLogin.xaml
    /// </summary>
    public partial class FrmLogin : Window {
        public UsuarioModel Usuario {
            get; set;
        }
        public UsuarioModel usrlog {
            get;
            private set;
        }
        public FrmLogin() {
            InitializeComponent();
            txbMensaje.Text = string.Empty;
            Usuario = new UsuarioModel();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            string c = UsuarioModel.cifrado(Usuario);
            usrlog = new UsuarioModel() {
                Usuario = txtUsuario.Text.ToLower(),
                Password = txtPass.Password,
                Salt = Usuario.Salt
            };

            if (Usuario.Usuario.Equals(usrlog.Usuario) &&
                Usuario.Password.Equals(UsuarioModel.cifrado(usrlog))) {
                this.DialogResult = true;
                this.Close();
            } else {
                //this.DialogResult = false;
                //this.Close();
                txbMensaje.Text = "Usuario y/o contraseña incorrectos.";
            }
        }

        private void txtUsuario_TextChanged(object sender, TextChangedEventArgs e) {
            txbMensaje.Text = string.Empty;
        }

        private void txtPass_PasswordChanged(object sender, RoutedEventArgs e) {
            txbMensaje.Text = string.Empty;
        }
    }
}

