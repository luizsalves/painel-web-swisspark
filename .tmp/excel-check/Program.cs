using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using PainelWebSwissPark.Api.Services.BusinessLogic;
using PainelWebSwissPark.Api.DTOs.Requests;
var r = new ExportarFechamentoRequest {
 Empresa="EMPRESA TESTE", Periodo="2026-07", CodColigada=8, CodFilial=1,
 CodConta="1.1.1.02.01.0001", DescricaoConta="Banco", PreparadoPor="Ana", ConferidoPor="Bruno", FinalizadoPor="Carla",
 SaldoAnterior=32436.55m, SaldoRazao=20349.98m,
 Lancamentos=[new(){Data="08/07/2026",IdPartida="001",Historico="=1+1",Valor=100m,Tipo="Débito"},new(){Data="09/07/2026",IdPartida="002",Historico="Credito",Valor=-84.2m,Tipo="Crédito"}]
};
void Check(bool ok,string msg){if(!ok) throw new Exception(msg);}
foreach(var count in new[]{2,0,300}) {
 var request=r;
 request.Lancamentos=count==0 ? [] : Enumerable.Range(0,count).Select(i=>new LinhaExportacaoFechamento{Data="08/07/2026",IdPartida="0001",Historico=i==0?"=1+1":new string('a',180),Valor=i%2==0?100m:-84.2m,Tipo=i%2==0?"Débito":"Crédito"}).ToList();
 request.PreparadoEm = count == 0 ? null : "07/10/2026, 00:15";
 request.ConferidoEm = count == 0 ? null : "07/10/2026, 09:30";
 request.FinalizadoEm = count == 0 ? null : "08/10/2026, 11:45";
 var bytes=FechamentoExcel.Gerar(request);
 Check(bytes[0]==0xD0 && bytes[1]==0xCF,"Formato XLS");
 using var book=new HSSFWorkbook(new MemoryStream(bytes));var sheet=book.GetSheetAt(0);
 Check(sheet.GetRow(12).GetCell(0).StringCellValue == "Em: " + (request.PreparadoEm ?? "—"), "Data preparo");
 Check(sheet.GetRow(12).GetCell(3).StringCellValue == "Em: " + (request.ConferidoEm ?? "—"), "Data conferencia");
 Check(sheet.GetRow(12).GetCell(6).StringCellValue == "Em: " + (request.FinalizadoEm ?? "—"), "Data finalizacao");
 Check(book.GetAllPictures().Count==1,"Logo ausente");
 Check(sheet.GetRow(11).GetCell(0).StringCellValue=="Ana","Preparador");
 Check(sheet.GetRow(11).GetCell(3).StringCellValue=="Bruno","Conferente");
 Check(sheet.GetRow(11).GetCell(6).StringCellValue=="Carla","Finalizador");
 if(count>0){Check(sheet.GetRow(21).GetCell(2).CellType==CellType.String,"Formula injection");Check(sheet.GetRow(21).GetCell(1).StringCellValue=="0001","Id textual");}
 int totalRow=22+Math.Max(1,count);
 Check(Math.Abs(sheet.GetRow(totalRow).GetCell(6).NumericCellValue-count/2*100d)<.001,"Total debito");
 Check(Math.Abs(sheet.GetRow(totalRow+1).GetCell(6).NumericCellValue-count/2*-84.2d)<.001,"Total credito");
 Check(sheet.PrintSetup.FitWidth==1,"Impressao");
 File.WriteAllBytes($".tmp/excel-check/amostra-{count}.xls",bytes);
 Console.WriteLine($"PASS: {count} lancamentos, XLS reaberto, logo, responsaveis, totais e texto literal.");
}
