using Ptap.Core;

if (args.Contains("--verificar") || args.Contains("--caso"))
{
    var fallos = 0;
    foreach (var c in Motor.VerificarEntregables())
    {
        Console.WriteLine($"{(c.Pasa ? "OK " : "FALLO")}  {c.Nombre}  [{c.Detalle}]");
        if (!c.Pasa) fallos++;
    }

    if (args.Contains("--caso"))
    {
        Console.WriteLine();
        Caso();
    }

    Console.WriteLine(fallos == 0
        ? "Todas las comprobaciones del avance coinciden."
        : $"{fallos} comprobaciones no pasaron.");
    return fallos == 0 ? 0 : 1;
}

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("Laboratorio PTAP — Daniel Esteban Ortiz Pinzón");
Console.WriteLine("Reemplaza los PhET de molaridad, sales/azúcar y ácido-base,");
Console.WriteLine("y calcula coagulación, desinfección y gases de la potabilización.");
Console.WriteLine();

var seguir = true;
while (seguir)
{
    Console.WriteLine("1  Banco de soluciones (molaridad y saturación)");
    Console.WriteLine("2  Ácido-base y pH");
    Console.WriteLine("3  Planta de potabilización");
    Console.WriteLine("4  Gases: cloro, CO2 y oxígeno");
    Console.WriteLine("5  Estequiometría y equilibrio (módulo 3)");
    Console.WriteLine("6  Verificar números del entregable");
    Console.WriteLine("0  Salir");
    Console.Write("> ");
    var opcion = Console.ReadLine();
    if (opcion is null) break;
    switch (opcion.Trim())
    {
        case "1": Banco(); break;
        case "2": Acido(); break;
        case "3": Planta(); break;
        case "4": Gases(); break;
        case "5": Estequiometria(); break;
        case "6":
            foreach (var c in Motor.VerificarEntregables())
                Console.WriteLine($"{(c.Pasa ? "OK " : "FALLO")}  {c.Nombre}  [{c.Detalle}]");
            break;
        case "0":
            seguir = false;
            break;
        default:
            Console.WriteLine("Opción no reconocida.");
            break;
    }

    Console.WriteLine();
}

return 0;

static void Banco()
{
    Console.WriteLine("Solutos: sacarosa, nacl, cuso4, alumbre, naclo");
    var id = LeerTexto("Soluto", "cuso4");
    var soluto = Motor.Solutos.FirstOrDefault(s => s.Id == id) ?? Motor.SolutoPorId("cuso4");
    var n = Leer("Moles de soluto", 0.50);
    var v = Leer("Volumen de solución (L)", 1.00);
    var r = Motor.EvaluarMolaridad(soluto, n, v);
    Console.WriteLine($"{soluto.Tradicional} ({soluto.Formula}), M = {soluto.MasaMolar} g/mol");
    Console.WriteLine($"M teórica n/V     {r.MolaridadTeorica:0.00} mol/L");
    Console.WriteLine($"M en la solución  {r.MolaridadReal:0.00} mol/L");
    Console.WriteLine($"Disuelto          {r.MolesDisueltos:0.000} mol   ({r.Gramos:0.0} g añadidos)");
    Console.WriteLine($"Sólido            {r.MolesSolido:0.000} mol");
    Console.WriteLine(r.Saturado ? "Estado: SATURADO" : "Estado: insaturada, homogénea");
    Console.WriteLine(r.Clasificacion);
    Console.WriteLine($"Conductividad relativa {r.Conductividad:0} / 100  ({soluto.Electrolito})");
}

static void Acido()
{
    Console.WriteLine("Sistemas: agua, hcl, hclo, naoh, clo");
    var id = LeerTexto("Sistema", "hcl");
    var c = Leer("Concentración analítica (mol/L)", 0.10);
    var r = Motor.EvaluarAcidoBase(id, c);
    var esp = Motor.FraccionHclo(r.Ph);
    Console.WriteLine($"{r.Titulo}: pH {r.Ph:0.00}   pOH {r.POH:0.00}");
    Console.WriteLine($"[H+] {r.H:0.00E+0} mol/L    [OH-] {r.Oh:0.00E+0} mol/L");
    Console.WriteLine($"Fracción disociada {r.FraccionDisociada:P1}");
    Console.WriteLine($"A este pH el cloro libre sería {esp.Hclo:P0} HClO y {esp.Clo:P0} ClO-");
    Console.WriteLine(r.Nota);
}

static void Planta()
{
    var entrada = new EntradaPlanta(
        Leer("Turbiedad cruda (NTU)", 80),
        Leer("Alcalinidad (mg/L CaCO3)", 40),
        Leer("pH crudo", 7.2),
        Leer("Caudal (L/s)", 100),
        Leer("Dosis de alumbre (mg/L)", 30),
        Leer("Dosis de cal viva CaO (mg/L)", 0),
        Leer("Dosis de cloro (mg/L)", 1.5),
        Leer("Temperatura (°C)", 20),
        Leer("Altitud (m)", 2590));
    var r = Motor.SimularPlanta(entrada);
    Console.WriteLine($"Dosis orientativa por turbiedad   {r.Sugerida:0.0} mg/L de alumbre");
    Console.WriteLine($"Alcalinidad consumida             {r.DemandaAlk:0.00} mg/L CaCO3");
    Console.WriteLine($"Alcalinidad que aporta la cal     {r.AlcalinidadCal:0.00} mg/L CaCO3");
    Console.WriteLine($"Alcalinidad neta                  {r.AlkNeta:0.00} mg/L");
    Console.WriteLine($"Cal mínima si falta alcalinidad   {r.CalNecesariaMgL:0.00} mg/L CaO");
    Console.WriteLine($"Lodo Al(OH)3                      {r.LodoMgL:0.00} mg/L");
    Console.WriteLine($"CO2 liberado / equilibrio aire    {r.Co2MgL:0.00} / {r.Co2Equilibrio:0.00} mg/L");
    Console.WriteLine($"pH estimado                       {r.Ph:0.00}");
    Console.WriteLine($"HClO / ClO-                       {r.Hclo:P0} / {r.Clo:P0}");
    Console.WriteLine($"Aluminio residual (modelo)        {r.Aluminio:0.000} mg/L");
    Console.WriteLine($"Turbiedad tratada                 {r.TurbiedadFinal:0.00} NTU  (remoción {r.Remocion:P0})");
    Console.WriteLine($"Cloro {r.CloroGDia / 1000:0.00} kg/día; hipoclorito 15 % {r.HipoLDia:0.0} L/día");
    Console.WriteLine($"Presión {r.Presion:0.000} atm; O2 de saturación {r.Oxigeno:0.00} mg/L");
    if (r.CalorKw > 0.01)
        Console.WriteLine($"Calor al apagar la cal            {r.CalorKw:0.00} kW  (ΔH = {Motor.DeltaHCalKj} kJ/mol)");
}

static void Gases()
{
    var masa = Leer("Masa de Cl2 (g)", 1000);
    var t = Leer("Temperatura (°C)", 20);
    var alt = Leer("Altitud (m)", 0);
    var p = Motor.PresionAtm(alt);
    var vol = Motor.VolumenGasCloroL(masa, t, p);
    var liq = (masa / 1000) / Motor.DensidadCl2Liquido;
    var pv = Motor.PresionVaporCloroBar(t);
    Console.WriteLine($"n = {masa / Motor.MCl2:0.00} mol");
    Console.WriteLine($"P = {p:0.000} atm a {alt:0} m");
    Console.WriteLine($"V = nRT/P = {vol:0.0} L");
    Console.WriteLine($"El mismo cloro líquido ocupa {liq:0.000} L (densidad {Motor.DensidadCl2Liquido} kg/L).");
    Console.WriteLine($"Al vaporizar el volumen crece unas {vol / Math.Max(liq, 1e-9):0} veces.");
    Console.WriteLine($"Si el cilindro aún tiene líquido, P = presión de vapor = {pv:0.00} bar, no la del gas ideal.");
    if (t >= Motor.FusibleC)
        Console.WriteLine("ALERTA: a unos 70 °C el tapón fusible del cilindro está en la zona de fundido.");
    Console.WriteLine($"O2 de saturación {Motor.OxigenoSaturadoMgL(t, p):0.00} mg/L");
    Console.WriteLine($"CO2 en equilibrio con el aire {Motor.Co2EquilibrioMgL(t, p):0.00} mg/L");
}

static void Caso()
{
    const double dosis = 55;
    const double alk = 20;
    const double ph = 7.2;
    const double q = 100;
    var est = Motor.EvaluarEstequiometria(dosis, alk, q);
    var cao = Motor.CaoParaPh(7, ph, alk, dosis);
    var sin = Motor.EvaluarEquilibrio(ph, alk, dosis, 0, 1);
    var justa = Motor.EvaluarEquilibrio(ph, alk, dosis, est.CaoMinimoMgL, 1);
    var aPh7 = Motor.EvaluarEquilibrio(ph, alk, dosis, cao, 0);
    var con = Motor.EvaluarEstequiometria(dosis, alk + cao * 100 / Motor.MCao, q);
    Console.WriteLine("--- Caso módulo 3: 55 mg/L, alcalinidad 20 mg/L, pH 7,20, 100 L/s ---");
    Console.WriteLine($"n dosis {est.NAlumDosisMolL:E4} mol/L; n alk {est.NAlkDisponibleMolL:E4} mol/L; n reacciona {est.NAlumReactMolL:E4} mol/L");
    Console.WriteLine($"Al(OH)3 {est.Aloh3TeoricoMgL:F3} mg/L ({est.Aloh3KgDia:F3} kg/día); todo el alumbre daría {est.Aloh3SiTodoMgL:F3} mg/L");
    Console.WriteLine($"CO2 {est.Co2MgL:F3} mg/L ({est.Co2KgDia:F3} kg/día); exceso de alumbre {est.AlumExcesoKgDia:F3} kg/día; dosificado {est.AlumDosadoKgDia:F3} kg/día");
    Console.WriteLine($"CID {sin.Inicial.DicMolL:E4} mol/L; alfa {sin.Inicial.Alfa0:F4} {sin.Inicial.Alfa1:F4} {sin.Inicial.Alfa2:F4}");
    Console.WriteLine($"sin cal cerrado pH {sin.PhCerrado:F3} HClO {sin.HcloCerrado:P2} alfa0 {sin.Alfa0Cerrado:F3} Al {sin.AluminioCerrado:F3}");
    Console.WriteLine($"sin cal aireado pH {sin.PhAireado:F3} HClO {sin.HcloAireado:P2}");
    Console.WriteLine($"cal justa {est.CaoMinimoMgL:F3} cerrado pH {justa.PhCerrado:F3} aireado pH {justa.PhAireado:F3}");
    Console.WriteLine($"CaO a pH 7 = {cao:F3} mg/L; pH {aPh7.PhCerrado:F3}; rendimiento {con.RendimientoDosis:P2}; Al(OH)3 {con.Aloh3TeoricoMgL:F3} mg/L ({con.Aloh3KgDia:F3} kg/día)");
    Console.WriteLine($"CaO kg/día {(cao * q * 0.0864):F3}; Kp {Motor.KpCo2:F3}; Kc1 {Motor.Kc1Carbonato:E3}; Kc2 {Motor.Kc2Carbonato:E3}");
}

static void Estequiometria()
{
    var dosis = Leer("Dosis de alumbre (mg/L)", 55);
    var alk = Leer("Alcalinidad (mg/L CaCO3)", 20);
    var ph = Leer("pH inicial", 7.2);
    var q = Leer("Caudal (L/s)", 100);
    var cal = Leer("Cal viva CaO (mg/L)", 0);
    var aire = Leer("Fracción de H2CO3* aireada (0 a 1)", 0);
    var est = Motor.EvaluarEstequiometria(dosis, alk + cal * 100 / Motor.MCao, q);
    var eq = Motor.EvaluarEquilibrio(ph, alk, dosis, cal, aire);
    var caoPh7 = Motor.CaoParaPh(7, ph, alk, dosis);
    Console.WriteLine($"Reacción: Al2(SO4)3·14H2O + 3 Ca(HCO3)2 → 2 Al(OH)3 + 3 CaSO4 + 6 CO2 + 14 H2O");
    Console.WriteLine($"Limitante: {est.Limitante}. En exceso: {est.Exceso}.");
    Console.WriteLine($"Demanda de alcalinidad     {est.DemandaAlkMgL:0.00} mg/L CaCO3");
    Console.WriteLine($"Alumbre que reacciona      {est.AlumReaccionadoMgL:0.00} mg/L   ({est.NAlumReactMolL:0.00E+0} mol/L)");
    Console.WriteLine($"Alumbre en exceso          {est.AlumExcesoMgL:0.00} mg/L");
    Console.WriteLine($"Al(OH)3 teórico            {est.Aloh3TeoricoMgL:0.00} mg/L   ({est.NAloh3MolL:0.00E+0} mol/L)");
    Console.WriteLine($"Si reaccionara toda la dosis  {est.Aloh3SiTodoMgL:0.00} mg/L");
    Console.WriteLine($"Rendimiento de la dosis    {est.RendimientoDosis:P1}");
    Console.WriteLine($"CO2 de la reacción         {est.Co2MgL:0.00} mg/L");
    Console.WriteLine($"CaO mínimo (solo estequiometría)  {est.CaoMinimoMgL:0.00} mg/L");
    Console.WriteLine($"CaO para pH cerrado = 7    {caoPh7:0.00} mg/L");
    Console.WriteLine($"A {q:0} L/s: alumbre dosificado {est.AlumDosadoKgDia:0.00} kg/día; Al(OH)3 {est.Aloh3KgDia:0.00} kg/día");
    Console.WriteLine($"Kc(HClO) = {Motor.KcHclo:0.00E+0}    Kc1 = {Motor.Kc1Carbonato:0.00E+0}    Kc2 = {Motor.Kc2Carbonato:0.00E+0}");
    Console.WriteLine($"Kp de CO2(ac) ⇌ CO2(g) = {Motor.KpCo2:0.00} atm·L/mol a 25 °C");
    Console.WriteLine($"CID inicial {eq.Inicial.DicMolL:0.00E+0} mol/L; α0 {eq.Inicial.Alfa0:P1}  α1 {eq.Inicial.Alfa1:P1}  α2 {eq.Inicial.Alfa2:P1}");
    Console.WriteLine($"Cerrado: pH {eq.PhCerrado:0.00}  alcalinidad {eq.AlkFinMgL:0.00} mg/L  HClO {eq.HcloCerrado:P0}  α0 {eq.Alfa0Cerrado:P0}");
    Console.WriteLine($"Aireado: pH {eq.PhAireado:0.00}  CID {eq.DicAireado:0.00E+0} mol/L  HClO {eq.HcloAireado:P0}");
}

static double Leer(string etiqueta, double defecto)
{
    Console.Write($"{etiqueta} [{defecto.ToString(System.Globalization.CultureInfo.InvariantCulture)}]: ");
    var texto = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(texto)) return defecto;
    texto = texto.Trim().Replace(',', '.');
    return double.TryParse(texto, System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture, out var valor)
        ? valor
        : defecto;
}

static string LeerTexto(string etiqueta, string defecto)
{
    Console.Write($"{etiqueta} [{defecto}]: ");
    var texto = Console.ReadLine();
    return string.IsNullOrWhiteSpace(texto) ? defecto : texto.Trim().ToLowerInvariant();
}
