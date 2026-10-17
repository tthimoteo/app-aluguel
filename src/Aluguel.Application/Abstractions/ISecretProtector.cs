namespace Aluguel.Application.Abstractions;

/// <summary>
/// Protege segredos (ex.: senha do certificado A1) com cifragem só no backend.
/// A senha em claro nunca deve retornar à API/frontend.
/// </summary>
public interface ISecretProtector
{
    byte[] Protect(string plaintext);
    string Unprotect(byte[] protectedBytes);
}
