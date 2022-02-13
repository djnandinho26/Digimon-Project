/*
 * Criado por SharpDevelop.
 * Usuário: Adriano
 * Data: 1/12/2011
 * Hora: 0:17
 * 
 * Para alterar este modelo use Ferramentas | Opções | Codificação | Editar Cabeçalhos Padrão.
 */

using System;

namespace Yggdrasil.Network
{
	/// <summary>
	/// Description of ConnectionEventArgs.
	/// </summary>
	public class ClientEventArgs : EventArgs
    {
        public IClient? Client { get; set; } = null;

        public ClientEventArgs(IClient client)
        {
            this.Client = client ?? throw new ArgumentNullException("client");
        }

        public override string ToString()
        {
            return Client?.RemoteEndPoint != null
                ? Client.RemoteEndPoint.ToString()
                : "Not Connected";
        }
    }
}
