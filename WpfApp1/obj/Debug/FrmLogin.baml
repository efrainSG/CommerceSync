   M S B A M L   `   `   `  ����H  DSincronizador, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null!clr-namespace:WpfApp1WpfApp1  5   	       WpfApp1.FrmLogin   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   'localclr-namespace:WpfApp1     5   	     � Title$  FrmLogin��5   	   $	��350��6   $	��420��6'    � WindowStyle$
 None=�63   ���	#7FFFFFFF � AllowsTransparency .  � 
ResizeMode$ NoResize=�6w    � WindowStartupLocation$ CenterScreen=�6�   .��5	      �  +� FocusedElement 6   ��  �� ElementName$ 
txtUsuario����5
   
   �� 5       �� EndPoint$ 0.5,1&�6"    �� 
StartPoint$ 0.5,0&�63   .��5      � 	 � Color$	 White��6   
 � Offset$
 0.431Q�6-   � 5      $
 1Q�6   $	 	#7FFFFFFF��6*   5      .+�5   
   �� -   5        $��
txbMensaje��5      $��Mensaje��6&   $��64.922,170.775,0,0q�65   $	��305��6Q   $
��Left=�6]   $	��Top=�6x      
errorBlock#���   6�   �� 5   
   $��Usuario:��6   $��64.922,191.775,0,0q�6#   $	��100��6?   $
��Left=�6K   $	��Top=�6f   �� 5   
   $��Contraseña:��6   $��64.922,234.775,0,0q�6&   $	��100��6B   $
��Left=�6N   $	��Top=�6i   �� 5   
   -   5        $��
txtUsuario��5      $��169.922,191.775,0,0q�6$   $	��200��6A   $
��Left=�6M   $	��Top=�6h   $��28��6�   6�   G� 5   
   -   5        $��txtPass��5      $��169.922,234.775,0,0q�6%   $	��200��6B   $
��Left=�6N   $	��Top=�6i   $��28��6�   6�   �� 5   
   $��Cerrar��6   $��204.922,278.775,0,0q�6"   $
��Left=�6?   $	��Top=�6Z     	negButton#���  6r    �� IsCancel . 6�   �� 5   
   -   5        $��Iniciar sesión��5      $��289.922,278.775,0,0q�6*   $
��Left=�6G   $	��Top=�6b    �� 	IsDefault . 6�   �� 5   
   $
��Left=�6   $	��130��6+   $��	10,10,0,0q�68   $	��Top=�6K   $	��400��6c   $��	logo.jpeg��6o   5      5      