using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Leiaute
{
    /// <summary>
    /// Extração dos layouts do safx-reforma-layout.json sem depender de biblioteca
    /// externa. Lê só o necessário: nome do layout, ordem dos campos, tamanho e o
    /// marcador "ordemConfirmada".
    /// </summary>
    internal sealed class JsonMinimo
    {
        private readonly string _texto;
        private int _pos;

        private JsonMinimo(string texto)
        {
            _texto = texto;
        }

        internal static Dictionary<string, LayoutReforma.Layout> Layouts(string json)
        {
            try
            {
                JsonMinimo parser = new JsonMinimo(json);
                Dictionary<string, object> mapa = (Dictionary<string, object>)parser.Valor();
                Dictionary<string, object> declarados = (Dictionary<string, object>)mapa["layouts"];

                Dictionary<string, LayoutReforma.Layout> resultado =
                    new Dictionary<string, LayoutReforma.Layout>(StringComparer.Ordinal);

                foreach (KeyValuePair<string, object> e in declarados)
                {
                    Dictionary<string, object> corpo = (Dictionary<string, object>)e.Value;
                    List<LayoutReforma.Campo> campos = new List<LayoutReforma.Campo>();
                    foreach (object o in (List<object>)corpo["fields"])
                    {
                        Dictionary<string, object> f = (Dictionary<string, object>)o;
                        campos.Add(new LayoutReforma.Campo(
                            Texto(f, "campo"),
                            Texto(f, "origem"),
                            Texto(f, "tipo"),
                            Texto(f, "tamanho")));
                    }
                    object confirmada = Valor(corpo, "ordemConfirmada");
                    object oficial = Valor(corpo, "fieldCountOficial");
                    resultado[e.Key] = new LayoutReforma.Layout(
                        e.Key,
                        campos,
                        confirmada is bool && (bool)confirmada,
                        oficial is double ? (int)(double)oficial : campos.Count);
                }
                return resultado;
            }
            catch (Exception ex)
            {
                throw new IOException("Falha ao ler " + LayoutReforma.NomeArquivo + ": " + ex.Message, ex);
            }
        }

        private static object Valor(Dictionary<string, object> mapa, string chave)
        {
            object v;
            return mapa.TryGetValue(chave, out v) ? v : null;
        }

        private static string Texto(Dictionary<string, object> mapa, string chave)
        {
            object v = Valor(mapa, chave);
            return v == null ? "" : Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        private object Valor()
        {
            Espacos();
            char c = _texto[_pos];
            switch (c)
            {
                case '{': return Objeto();
                case '[': return Lista();
                case '"': return Cadeia();
                case 't': _pos += 4; return true;
                case 'f': _pos += 5; return false;
                case 'n': _pos += 4; return null;
                default: return Numero();
            }
        }

        private Dictionary<string, object> Objeto()
        {
            Dictionary<string, object> mapa = new Dictionary<string, object>(StringComparer.Ordinal);
            _pos++;
            Espacos();
            if (_texto[_pos] == '}') { _pos++; return mapa; }
            while (true)
            {
                Espacos();
                string chave = Cadeia();
                Espacos();
                _pos++; // :
                mapa[chave] = Valor();
                Espacos();
                if (_texto[_pos++] == '}') return mapa;
            }
        }

        private List<object> Lista()
        {
            List<object> itens = new List<object>();
            _pos++;
            Espacos();
            if (_texto[_pos] == ']') { _pos++; return itens; }
            while (true)
            {
                itens.Add(Valor());
                Espacos();
                if (_texto[_pos++] == ']') return itens;
            }
        }

        private string Cadeia()
        {
            StringBuilder sb = new StringBuilder();
            _pos++;
            while (true)
            {
                char c = _texto[_pos++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                char e = _texto[_pos++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'u':
                        sb.Append((char)int.Parse(_texto.Substring(_pos, 4),
                            NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        _pos += 4;
                        break;
                    default: sb.Append(e); break;
                }
            }
        }

        private double Numero()
        {
            int inicio = _pos;
            while (_pos < _texto.Length && "-+.eE0123456789".IndexOf(_texto[_pos]) >= 0) _pos++;
            return double.Parse(_texto.Substring(inicio, _pos - inicio), CultureInfo.InvariantCulture);
        }

        private void Espacos()
        {
            while (_pos < _texto.Length && char.IsWhiteSpace(_texto[_pos])) _pos++;
        }
    }
}
