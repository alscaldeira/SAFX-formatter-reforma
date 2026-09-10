//PROJETO ANTIGO
using ABMC.RANFE.LIGHT_SQL.DAO;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace ABMC.RANFE.LIGHT_SQL.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(List<HttpPostedFileBase> iFile, string empresa)
        {
            string path = base.Server.MapPath("~/UploadXML");
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            FileInfo[] files = directoryInfo.GetFiles();

            foreach (FileInfo fileInfo in files)
            {
                System.IO.File.Delete(fileInfo.FullName);
            }

            foreach (HttpPostedFileBase item in iFile)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item.FileName);
                fileNameWithoutExtension += Path.GetExtension(item.FileName);
                item.SaveAs(base.Server.MapPath("~/uploadxml/") + fileNameWithoutExtension);
            }

            LerXml lerXml = new LerXml("arquivo");
            lerXml.Integra = true;
            lerXml.GravaSynchro = false;
            DAO_SAFX.ClearAllRecords();

            Funcoes.CarregarArquivo1(lerXml);
            Funcoes.NormalizarXMLNFe();

            string comando = "";
            string comando2 = "";
            string comando3 = "";
            string comando4 = "";
            string comando5 = "";

            using (StreamReader streamReader = new StreamReader(base.Server.MapPath("~/ScriptSQL/SAFX07.txt")))
            {
                comando = streamReader.ReadToEnd(); //Capa
            }
            
            using (StreamReader streamReader = new StreamReader(base.Server.MapPath("~/ScriptSQL/SAFX08.txt")))
            {
                comando2 = streamReader.ReadToEnd(); //Linhas
            }
            
            using (StreamReader streamReader = new StreamReader(base.Server.MapPath("~/ScriptSQL/SAFX04.txt")))
            {
                comando3 = streamReader.ReadToEnd(); //Cadastro do emitente
            }
            
            using (StreamReader streamReader = new StreamReader(base.Server.MapPath("~/ScriptSQL/SAFX2013.txt")))
            {
                comando4 = streamReader.ReadToEnd(); //Produtos
            }
            
            using (StreamReader streamReader = new StreamReader(base.Server.MapPath("~/ScriptSQL/SAFX2043.txt")))
            {
                comando5 = streamReader.ReadToEnd(); //Codigos NBM 
            }

            DataTable dataTable = DAO_SAFX.Consulta(comando);
            DataTable dataTable2 = DAO_SAFX.Consulta(comando2);
            DataTable dataTable3 = DAO_SAFX.Consulta(comando3);
            DataTable dataTable4 = DAO_SAFX.Consulta(comando4);
            DataTable dataTable5 = DAO_SAFX.Consulta(comando5);
            
            string path2 = base.Server.MapPath("~/Export");
            DirectoryInfo directoryInfo2 = new DirectoryInfo(path2);
            files = directoryInfo2.GetFiles();

            foreach (FileInfo fileInfo in files)
            {
                System.IO.File.Delete(fileInfo.FullName);
            }

            // Extensoes da Reforma Tributaria (IBS/CBS): SAFX3007/3008/3009.
            //
            // Os SAFX07/08/04/2013/2043 acima saem do banco, via script SQL; os da
            // Reforma saem direto dos grupos IBSCBS do XML, que a carga do banco
            // nao guarda. Por isso a conversao le a mesma pasta ~/UploadXML que
            // acabou de ser carregada, e nao as tabelas int_nfe_*.
            //
            // A falha aqui nao pode derrubar a exportacao existente: o XML anterior
            // ao novo regime nao tem grupo IBS/CBS, e o cliente que ainda nao usa a
            // Reforma continua recebendo as demais abas normalmente. O que deu
            // errado vai para a tela, em ViewBag.AvisosReforma.
            ResultadoReforma reforma = null;
            List<string> avisosReforma = new List<string>();

            try
            {
                reforma = ConversorXmlReforma.Converter(
                    base.Server.MapPath("~/UploadXML"),
                    base.Server.MapPath("~/Export"),
                    base.Server.MapPath("~/ReformaTributaria/Dados"));
                avisosReforma.AddRange(reforma.Avisos);
            }
            catch (Exception ex)
            {
                avisosReforma.Add("Reforma Tributaria (SAFX3007/3008/3009) nao gerada: " + ex.Message);
            }

            var now = DateTime.Now;
            string text = $"{empresa}_{now.Year}_{now.Month:00}_{now.Day:00}_{now.Hour:00}{now.Minute:00}{now.Second:00}.xlsx";
            FileInfo newFile = new FileInfo(base.Server.MapPath("~/Export/" + text));

            using (ExcelPackage excelPackage = new ExcelPackage(newFile))
            {
                int num = 1;
                ExcelWorksheet excelWorksheet = excelPackage.Workbook.Worksheets.Add("SAFX07");
                
                for (int j = 0; j < dataTable.Columns.Count; j++)
                {
                    int num2 = 2;
                    excelWorksheet.Cells[1, num].Value = ((object)dataTable.Columns[j]).ToString();
                    
                    for (int k = 0; k < dataTable.Rows.Count; k++)
                    {
                        excelWorksheet.Cells[num2, num].Value = dataTable.Rows[k][j];
                        num2++;
                    }
                    
                    num++;
                }
                
                num = 1;
                ExcelWorksheet excelWorksheet2 = excelPackage.Workbook.Worksheets.Add("SAFX08");
                
                for (int j = 0; j < dataTable2.Columns.Count; j++)
                {
                    int num2 = 2;
                    excelWorksheet2.Cells[1, num].Value = ((object)dataTable2.Columns[j]).ToString();
                    
                    for (int k = 0; k < dataTable2.Rows.Count; k++)
                    {
                        object value = dataTable2.Rows[k][j];
                        if (value == null || string.IsNullOrWhiteSpace(value.ToString())) value = "@";
                        excelWorksheet2.Cells[num2, num].Value = value;
                        num2++;
                    }
                    
                    num++;
                }
                
                num = 1;
                ExcelWorksheet excelWorksheet3 = excelPackage.Workbook.Worksheets.Add("SAFX04");
                
                for (int j = 0; j < dataTable3.Columns.Count; j++)
                {
                    int num2 = 2;
                    excelWorksheet3.Cells[1, num].Value = ((object)dataTable3.Columns[j]).ToString();
                    
                    for (int k = 0; k < dataTable3.Rows.Count; k++)
                    {
                        excelWorksheet3.Cells[num2, num].Value = dataTable3.Rows[k][j];
                        if (j == 7 && !dataTable3.Rows[k][j].ToString().Equals("0"))
                        {
                            excelWorksheet3.Cells[num2, num].Style.Numberformat.Format = "0";
                        }
                        if (j == 19)
                        {
                            excelWorksheet3.Cells[num2, num].Style.Numberformat.Format = "@";
                        }
                        
                        num2++;
                    }
                    
                    num++;
                }
                
                num = 1;
                ExcelWorksheet excelWorksheet4 = excelPackage.Workbook.Worksheets.Add("SAFX2013");
                
                for (int j = 0; j < dataTable4.Columns.Count; j++)
                {
                    int num2 = 2;
                    excelWorksheet4.Cells[1, num].Value = ((object)dataTable4.Columns[j]).ToString();
                    
                    for (int k = 0; k < dataTable4.Rows.Count; k++)
                    {
                        excelWorksheet4.Cells[num2, num].Value = dataTable4.Rows[k][j];
                        num2++;
                    }
                    
                    num++;
                }
                
                num = 1;
                ExcelWorksheet excelWorksheet5 = excelPackage.Workbook.Worksheets.Add("SAFX2043");
                
                for (int j = 0; j < dataTable5.Columns.Count; j++)
                {
                    int num2 = 2;
                    excelWorksheet5.Cells[1, num].Value = ((object)dataTable5.Columns[j]).ToString();
                    
                    for (int k = 0; k < dataTable4.Rows.Count; k++)
                    {
                        excelWorksheet5.Cells[num2, num].Value = dataTable5.Rows[k][j];
                        num2++;
                    }
                    
                    num++;
                }
                
                if (reforma != null)
                {
                    // As tres abas da Reforma repetem o conteudo dos arquivos
                    // SAFX3007/3008/3009 ja gravados em ~/Export: mesmas colunas,
                    // mesmos valores (inclusive o "@" de campo nulo), para que a
                    // planilha e o arquivo de importacao nunca divirjam.
                    AdicionarAba(excelPackage, "SAFX3007", reforma.Safx3007);
                    AdicionarAba(excelPackage, "SAFX3008", reforma.Safx3008);
                    AdicionarAba(excelPackage, "SAFX3009", reforma.Safx3009);
                }

                excelPackage.Save();
            }
            
            base.ViewBag.NomeArquivo = text;
            base.ViewBag.AvisosReforma = avisosReforma;
            return View();
        }

        /// <summary>
        /// Uma aba por layout da Reforma: a primeira linha recebe os nomes de
        /// campo do leiaute e as seguintes, os registros. Todas as celulas sao
        /// gravadas como texto ("@" de formato), porque os campos do SAFX sao
        /// alfanumericos no leiaute mesmo quando so tem digitos — deixar o Excel
        /// interpretar comeria o zero a esquerda do CNPJ e do numero do documento
        /// e mandaria um valor de 17 digitos para notacao cientifica.
        /// </summary>
        private static void AdicionarAba(ExcelPackage pacote, string nome, DataTable dados)
        {
            if (dados == null) return;

            ExcelWorksheet aba = pacote.Workbook.Worksheets.Add(nome);

            for (int coluna = 0; coluna < dados.Columns.Count; coluna++)
            {
                // Formato de texto na COLUNA, nao celula a celula: o SAFX3007 tem
                // 130 campos e um lote de milhares de notas passa de meio milhao de
                // celulas. Aplicar estilo em cada uma leva dezenas de segundos e
                // estoura o executionTimeout do IIS; por coluna, o custo e o numero
                // de campos do leiaute.
                aba.Column(coluna + 1).Style.Numberformat.Format = "@";
                aba.Cells[1, coluna + 1].Value = dados.Columns[coluna].ColumnName;
            }

            for (int linha = 0; linha < dados.Rows.Count; linha++)
            {
                for (int coluna = 0; coluna < dados.Columns.Count; coluna++)
                {
                    aba.Cells[linha + 2, coluna + 1].Value = dados.Rows[linha][coluna];
                }
            }
        }

        public ActionResult Config()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Config(List<HttpPostedFileBase> iFile, string ddlTipoScript)
        {
            var now = DateTime.Now;
            var f = $"{now.Year}_{now.Month:00}_{now.Day:00}_{now.Hour:00}{now.Minute:00}{now.Second:00}.txt";

            if (ddlTipoScript.Equals("SAFX07"))
            {
                System.IO.File.Move(base.Server.MapPath("~/ScriptSQL/SAFX07.txt"), base.Server.MapPath("~/ScriptSQL/Versions/SAFX07_" + f));
                
                foreach (HttpPostedFileBase item in iFile)
                {
                    item.SaveAs(base.Server.MapPath("~/ScriptSQL/SAFX07.txt"));
                }
            }
            else if (ddlTipoScript.Equals("SAFX08"))
            {
                System.IO.File.Move(base.Server.MapPath("~/ScriptSQL/SAFX08.txt"), base.Server.MapPath("~/ScriptSQL/Versions/SAFX08_" + f));
                
                foreach (HttpPostedFileBase item2 in iFile)
                {
                    item2.SaveAs(base.Server.MapPath("~/ScriptSQL/SAFX08.txt"));
                }
            }
            
            return View();
        }

        public ActionResult CargaExcel()
        {
            return (ActionResult)this.View();
        }

        [HttpPost]
        public ActionResult CargaExcel(List<HttpPostedFileBase> iFile)
        {
            foreach (FileSystemInfo file in new DirectoryInfo(this.Server.MapPath("~/UploadXML")).GetFiles())
                System.IO.File.Delete(file.FullName);
            string str = "";
            
            foreach (HttpPostedFileBase httpPostedFileBase in iFile)
            {
                str = Path.GetFileNameWithoutExtension(httpPostedFileBase.FileName);
                str += Path.GetExtension(httpPostedFileBase.FileName);
                httpPostedFileBase.SaveAs(this.Server.MapPath("~/UploadXML/") + str);
            }
            
            DataTable dataTable = HomeController.exceldata(this.Server.MapPath("~/UploadXML/") + str);
            
            for (int index = 0; index < dataTable.Rows.Count; ++index)
            {
                if (dataTable.Rows[index][1].ToString() != "")
                    System.IO.File.WriteAllText(this.Server.MapPath("/UploadXML/") + (dataTable.Rows[index]["CHAVE_ACESSO"].ToString() + ".xml"), dataTable.Rows[index]["XML"].ToString());
            }
            
            return (ActionResult)this.View();
        }

        private static DataTable exceldata(string filePath)
        {
            DataTable dataTable = new DataTable();
            string str1 = false ? "Yes" : "No";
            string connectionString;
            
            if (filePath.Substring(filePath.LastIndexOf('.')).ToLower() == ".xlsx")
                connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath + ";Extended Properties=\"Excel 12.0;HDR=" + str1 + ";IMEX=0\"";
            else
                connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filePath + ";Extended Properties=\"Excel 8.0;HDR=" + str1 + ";IMEX=0\"";
            
            OleDbConnection selectConnection = new OleDbConnection(connectionString);
            selectConnection.Open();
            
            string str2 = selectConnection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[4]
            {
        null,
        null,
        null,
        (object) "TABLE"
            }).Rows[0]["TABLE_NAME"].ToString();
            
            if (!str2.EndsWith("_"))
            {
                OleDbDataAdapter oleDbDataAdapter = new OleDbDataAdapter("SELECT  * FROM [" + str2 + "]", selectConnection);
                dataTable.Locale = CultureInfo.CurrentCulture;
                oleDbDataAdapter.Fill(dataTable);
            }
            
            selectConnection.Close();
            return dataTable;
        }
    }
}
