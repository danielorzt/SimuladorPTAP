namespace Ptap.Core;

/// <summary>
/// Motor del simulador de potabilización.
/// Misma estequiometría que el avance 2 (alumbre comercial 594 g/mol)
/// y los bancos que reemplazan los PhET de molaridad, sales/azúcar y ácido-base.
/// </summary>
public static class Motor
{
    public const double MAlumbre = 594;
    public const double MAloh3 = 78;
    public const double MCo2 = 44;
    public const double MCao = 56;
    public const double MCl2 = 70.9;
    public const double RLatm = 0.08205746;
    public const double PkaHclo = 7.5;
    public const double Kw = 1e-14;
    public const double DeltaHCalKj = -65.2;
    public const double AlkPorMgAlumbre = 300.0 / MAlumbre;
    public const double LodoPorMgAlumbre = (2 * MAloh3) / MAlumbre;
    public const double Co2PorMgAlk = (6 * MCo2) / 300.0;
    public const double CloroDisponible15GL = 143;
    public const double DensidadCl2Liquido = 1.41;
    public const double FusibleC = 70;
    public const double Pka1 = 6.35;
    public const double Pka2 = 10.33;
    /// <summary>mol/(L·atm) a 25 °C. Kp de CO₂(ac) ⇌ CO₂(g) es 1/KH.</summary>
    public const double KHCo2 = 0.034;

    public static double KcHclo => Math.Pow(10, -PkaHclo);
    public static double Kc1Carbonato => Math.Pow(10, -Pka1);
    public static double Kc2Carbonato => Math.Pow(10, -Pka2);
    public static double KpCo2 => 1.0 / KHCo2;

    public static readonly Soluto[] Solutos =
    [
        new("sacarosa", "C12H22O11", "Azúcar", "Sacarosa", "No electrolito", 342.3, 5.85, 0),
        new("nacl", "NaCl", "Sal común", "Cloruro de sodio", "Electrolito fuerte", 58.44, 6.14, 2),
        new("cuso4", "CuSO4", "Sulfato de cobre", "Sulfato de cobre(II)", "Electrolito fuerte", 159.61, 1.40, 2),
        new("alumbre", "Al2(SO4)3·14H2O", "Sulfato de aluminio", "Sulfato de aluminio(III)", "Electrolito fuerte", 594, 1.10, 5),
        new("naclo", "NaClO", "Hipoclorito de sodio", "Clorato(I) de sodio", "Electrolito fuerte", 74.44, 6.50, 2),
    ];

    public static Soluto SolutoPorId(string id) =>
        Solutos.First(s => s.Id == id);

    public static ResultadoMolaridad EvaluarMolaridad(Soluto soluto, double moles, double volumenL)
    {
        var v = Math.Max(volumenL, 1e-6);
        var n = Math.Max(moles, 0);
        var teorica = n / v;
        var capacidad = soluto.SolubilidadMolL * v;
        var disueltos = Math.Min(n, capacidad);
        var solido = Math.Max(0, n - disueltos);
        var real = disueltos / v;
        var saturado = solido > 1e-6 || real >= soluto.SolubilidadMolL - 1e-4;
        var clasificacion = saturado
            ? "Mezcla heterogénea: solución saturada con sólido sin disolver"
            : n <= 1e-8
                ? "Sustancia pura: solo agua, sin soluto añadido"
                : "Mezcla homogénea: solución insaturada";
        return new ResultadoMolaridad(teorica, real, disueltos, solido, n * soluto.MasaMolar, saturado, clasificacion, Conductividad(soluto.Iones * real));
    }

    public static double Conductividad(double concentracionIones)
    {
        if (concentracionIones <= 1e-8) return 3;
        return Math.Min(100, 28 * Math.Log10(concentracionIones * 500 + 1));
    }

    public static ResultadoAcido EvaluarAcidoBase(string id, double cIn)
    {
        var c = Math.Max(0, cIn);
        if (id == "agua" || c == 0)
        {
            return new ResultadoAcido(7, 7, 1e-7, 1e-7, 1, 4, "Agua",
                "Sustancia pura en equilibrio. Kw = 1e-14.");
        }

        if (id == "hcl")
        {
            var h = (c + Math.Sqrt(c * c + 4 * Kw)) / 2;
            var oh = Kw / h;
            return new ResultadoAcido(-Math.Log10(h), -Math.Log10(oh), h, oh, 1, Conductividad(h + c + oh),
                "HCl", "Ácido fuerte. Es el HCl de la hidrólisis del Cl2.");
        }

        if (id == "naoh")
        {
            var oh = (c + Math.Sqrt(c * c + 4 * Kw)) / 2;
            var h = Kw / oh;
            return new ResultadoAcido(-Math.Log10(h), -Math.Log10(oh), h, oh, 1, Conductividad(oh + c + h),
                "NaOH", "Base fuerte. 0,10 mol/L da pH 13.");
        }

        if (id == "hclo")
        {
            var ka = Math.Pow(10, -PkaHclo);
            var x = (-ka + Math.Sqrt(ka * ka + 4 * ka * c)) / 2;
            var h = Math.Max(x, 1e-10);
            var oh = Kw / h;
            var a = Math.Min(c, x);
            return new ResultadoAcido(-Math.Log10(h), -Math.Log10(oh), h, oh, c > 0 ? a / c : 0,
                Conductividad(h + a + oh), "HClO", "Ácido débil, pKa 7,5. Desinfecta mejor que ClO-.");
        }

        var kb = Kw / Math.Pow(10, -PkaHclo);
        var y = (-kb + Math.Sqrt(kb * kb + 4 * kb * c)) / 2;
        var ohb = Math.Max(y, 1e-10);
        var hb = Kw / ohb;
        var bh = Math.Min(c, y);
        return new ResultadoAcido(-Math.Log10(hb), -Math.Log10(ohb), hb, ohb, c > 0 ? bh / c : 0,
            Conductividad(ohb + bh + hb + Math.Max(0, c - bh)), "ClO-",
            "Hidrólisis del hipoclorito. El cloro permanece en +1: no es redox.");
    }

    public static (double Hclo, double Clo) FraccionHclo(double ph, double pKa = PkaHclo)
    {
        var r = Math.Pow(10, ph - pKa);
        var hclo = 1.0 / (1 + r);
        return (hclo, 1 - hclo);
    }

    public static double PresionAtm(double altitudM)
    {
        var h = Math.Clamp(altitudM, -200, 11000);
        return Math.Pow(1 - 2.25577e-5 * h, 5.25588);
    }

    public static double VolumenGasCloroL(double masaG, double tempC, double pAtm)
    {
        var n = Math.Max(masaG, 0) / MCl2;
        var p = Math.Max(pAtm, 1e-6);
        return n * RLatm * (tempC + 273.15) / p;
    }

    /// <summary>log10(P/bar) = 4,28814 − 969,992/(T − 12,791). NIST, Stull 1947.</summary>
    public static double PresionVaporCloroBar(double tempC)
    {
        var t = tempC + 273.15;
        var log10p = 4.28814 - 969.992 / (t - 12.791);
        return Math.Pow(10, log10p);
    }

    public static double PresionVaporAguaAtm(double tempC)
    {
        var log10mm = 8.07131 - 1730.63 / (233.426 + tempC);
        return Math.Pow(10, log10mm) / 760.0;
    }

    public static double OxigenoSaturadoMgL(double tempC, double pAtm)
    {
        var ta = tempC + 273.15;
        var ln = -139.34411
                 + 1.575701e5 / ta
                 - 6.642308e7 / (ta * ta)
                 + 1.243800e10 / (ta * ta * ta)
                 - 8.621949e11 / (ta * ta * ta * ta);
        var agua = PresionVaporAguaAtm(tempC);
        var factor = Math.Max(0, pAtm - agua) / Math.Max(1e-6, 1 - agua);
        return Math.Exp(ln) * factor;
    }

    public static double Co2EquilibrioMgL(double tempC, double pAtm, double ppm = 420)
    {
        const double kH25 = 0.034;
        var t = tempC + 273.15;
        var kH = kH25 * Math.Exp(2400 * (1 / t - 1 / 298.15));
        var pCo2 = ppm / 1e6 * pAtm;
        return kH * pCo2 * MCo2 * 1000;
    }

    public static double DosisOrientativa(double ntu)
    {
        var t = Math.Max(0, ntu);
        if (t <= 10) return 10;
        if (t <= 180) return Lerp(10, 35, (t - 10) / (180 - 10));
        if (t <= 840) return Lerp(35, 55, (t - 180) / (840 - 180));
        if (t <= 1000) return Lerp(55, 100, (t - 840) / (1000 - 840));
        return 100 + (t - 1000) * 0.04;
    }

    public static double EstimarPh(double phCrudo, double alk, double alum, double cal)
    {
        var demand = AlkPorMgAlumbre * Math.Max(alum, 0);
        var limeAlk = Math.Max(cal, 0) * 100 / MCao;
        var net = alk + limeAlk - demand;
        if (net < 0.05)
        {
            var acidity = Math.Max(1e-7, -net / 50_000);
            return Math.Clamp(-Math.Log10(acidity), 2, 7);
        }

        var consumida = Math.Min(Math.Max(alk, 0), demand);
        var fraccion = alk <= 0 ? 1 : consumida / alk;
        var ph = phCrudo - 0.85 * fraccion;
        var calParaCubrir = Math.Max(0, demand - Math.Max(alk, 0));
        var exceso = Math.Max(0, limeAlk - calParaCubrir);
        if (exceso > 1)
        {
            var ohAlk = Math.Max(0, exceso - 8);
            if (ohAlk > 0.2)
            {
                var oh = ohAlk / 50_000;
                ph = Math.Max(ph, 14 + Math.Log10(oh));
            }
            else ph += 0.12 * exceso;
        }

        return Math.Clamp(ph, 2, 12.5);
    }

    public static double AluminioResidual(double ph)
    {
        var dist = ph - 6.7;
        var amph = 0.02 + 0.08 * dist * dist;
        var extra = ph < 5.5 ? (5.5 - ph) * 0.25 : ph > 8.5 ? (ph - 8.5) * 0.12 : 0;
        return Math.Min(2.5, amph + extra);
    }

    public static double RemocionTurbiedad(double dosis, double sugerida, double ph, double alkNeta)
    {
        var ratio = sugerida <= 0 ? 1 : dosis / sugerida;
        if (ratio >= 0.75 && ratio <= 1.6 && ph >= 6.2 && ph <= 7.8 && alkNeta >= 5) return 0.97;
        if (ratio >= 0.45 && ratio <= 2.2 && ph >= 5.8 && ph <= 8.3 && alkNeta >= 0) return 0.82;
        if (ratio < 0.45) return Math.Max(0.15, 0.25 + ratio);
        return 0.5;
    }

    public static ResultadoPlanta SimularPlanta(EntradaPlanta e)
    {
        var demanda = AlkPorMgAlumbre * e.DosisAlumbreMgL;
        var alkCal = e.DosisCalMgL * 100 / MCao;
        var alkNeta = e.AlcalinidadMgL + alkCal - demanda;
        var bicarb = Math.Min(Math.Max(e.AlcalinidadMgL, 0), Math.Max(demanda, 0));
        var calNecesaria = Math.Max(0, demanda - e.AlcalinidadMgL) * MCao / 100;
        var co2 = bicarb * Co2PorMgAlk;
        var p = PresionAtm(e.AltitudM);
        var ph = EstimarPh(e.PhCrudo, e.AlcalinidadMgL, e.DosisAlumbreMgL, e.DosisCalMgL);
        var sugerida = DosisOrientativa(e.TurbiedadNtu);
        var rem = RemocionTurbiedad(e.DosisAlumbreMgL, sugerida, ph, alkNeta);
        var esp = FraccionHclo(ph);
        var cloroGDia = e.CaudalLs * e.DosisCloroMgL * 86.4;
        var molesCal = e.DosisCalMgL / 1000 / MCao;
        return new ResultadoPlanta(
            demanda,
            alkCal,
            alkNeta,
            bicarb,
            calNecesaria,
            co2,
            Co2EquilibrioMgL(e.TemperaturaC, p),
            LodoPorMgAlumbre * e.DosisAlumbreMgL,
            ph,
            AluminioResidual(ph),
            sugerida,
            rem,
            e.TurbiedadNtu * (1 - rem),
            esp.Hclo,
            esp.Clo,
            cloroGDia,
            cloroGDia / CloroDisponible15GL,
            e.CaudalLs * molesCal * Math.Abs(DeltaHCalKj),
            p,
            OxigenoSaturadoMgL(e.TemperaturaC, p));
    }

    /// <summary>
    /// Coagulación por litro. 1 mol de alumbre exige 3 mol de alcalinidad como CaCO₃ (300 g)
    /// y, si el bicarbonato alcanza, produce 2 mol de Al(OH)₃ y 6 mol de CO₂.
    /// </summary>
    public static ResultadoEsteq EvaluarEstequiometria(double dosisAlumbreMgL, double alcalinidadMgL, double caudalLs)
    {
        var dosis = Math.Max(0, dosisAlumbreMgL);
        var alk = Math.Max(0, alcalinidadMgL);
        var q = Math.Max(0, caudalLs);
        var demanda = AlkPorMgAlumbre * dosis;
        var alkUsada = Math.Min(alk, demanda);
        var fraccion = demanda <= 1e-12 ? 0 : alkUsada / demanda;
        var alumReaccionado = dosis * fraccion;
        var alumExceso = dosis - alumReaccionado;
        var alkExceso = alk - demanda;
        var limitanteAlk = alumExceso > 0.05 && alkUsada + 1e-9 >= alk && dosis > 0;
        var nDosis = dosis / (MAlumbre * 1000);
        var nReact = alumReaccionado / (MAlumbre * 1000);
        var nAlk = alk / (100 * 1000.0);
        var nAloh3 = 2 * nReact;
        var aloh3 = nAloh3 * MAloh3 * 1000;
        var aloh3Todo = dosis * (2 * MAloh3) / MAlumbre;
        var co2 = 6 * nReact * MCo2 * 1000;
        var deficit = Math.Max(0, demanda - alk);
        var kg = q * 0.0864;
        string limitante;
        string exceso;
        if (dosis <= 1e-9)
        {
            limitante = "No hay reacción: la dosis de alumbre es cero";
            exceso = "Alcalinidad sin consumir";
        }
        else if (Math.Abs(alk - demanda) <= 0.05)
        {
            limitante = "Estequiometría justa: se agotan alumbre y alcalinidad";
            exceso = "Ninguno";
        }
        else if (limitanteAlk)
        {
            limitante = "Alcalinidad, como Ca(HCO₃)₂";
            exceso = "Alumbre comercial";
        }
        else
        {
            limitante = "Alumbre comercial, Al₂(SO₄)₃·14H₂O";
            exceso = "Alcalinidad";
        }

        return new ResultadoEsteq(
            limitanteAlk,
            limitante,
            exceso,
            demanda,
            alkUsada,
            alkExceso,
            alumReaccionado,
            alumExceso,
            nDosis,
            nReact,
            nAlk,
            nAloh3,
            aloh3,
            aloh3Todo,
            aloh3Todo <= 1e-12 ? 0 : aloh3 / aloh3Todo,
            co2,
            deficit * MCao / 100,
            alumReaccionado * kg,
            alumExceso * kg,
            aloh3 * kg,
            co2 * kg,
            dosis * kg);
    }

    public static Carbonato EstadoCarbonato(double ph, double alcalinidadMgL)
    {
        var h = Math.Pow(10, -ph);
        var oh = Kw / h;
        var ka1 = Kc1Carbonato;
        var ka2 = Kc2Carbonato;
        var den = h * h + ka1 * h + ka1 * ka2;
        var a0 = h * h / den;
        var a1 = ka1 * h / den;
        var a2 = ka1 * ka2 / den;
        var alkEq = alcalinidadMgL / 50_000.0;
        var factor = a1 + 2 * a2;
        var dic = factor > 1e-15 ? (alkEq - oh + h) / factor : 0;
        if (dic < 0) dic = 0;
        return new Carbonato(ph, alcalinidadMgL, alkEq, dic, a0, a1, a2, dic * a0, dic * a1, dic * a2, h, oh);
    }

    /// <summary>Resuelve pH a partir de alcalinidad (eq/L) y CID (mol/L). Bisección sobre la definición de alcalinidad.</summary>
    public static double PhDesdeAlkDic(double alkEq, double dicMol)
    {
        var dic = Math.Max(0, dicMol);
        double F(double ph)
        {
            var h = Math.Pow(10, -ph);
            var oh = Kw / h;
            var ka1 = Kc1Carbonato;
            var ka2 = Kc2Carbonato;
            var den = h * h + ka1 * h + ka1 * ka2;
            var a1 = ka1 * h / den;
            var a2 = ka1 * ka2 / den;
            return dic * (a1 + 2 * a2) + oh - h - alkEq;
        }

        var lo = 1.5;
        var hi = 13.0;
        if (F(lo) > 0) return lo;
        if (F(hi) < 0) return hi;
        for (var i = 0; i < 64; i++)
        {
            var mid = (lo + hi) / 2;
            if (F(mid) > 0) hi = mid;
            else lo = mid;
        }

        return (lo + hi) / 2;
    }

    /// <summary>CaO (mg/L) para que el sistema cerrado quede en el pH pedido después de dosificar el alumbre.</summary>
    public static double CaoParaPh(double phObjetivo, double ph0, double alcalinidadMgL, double dosisAlumbreMgL)
    {
        var inicial = EstadoCarbonato(ph0, alcalinidadMgL);
        var demanda = AlkPorMgAlumbre * Math.Max(dosisAlumbreMgL, 0);
        double PhDe(double cao)
        {
            var alkFin = alcalinidadMgL + cao * 100 / MCao - demanda;
            return PhDesdeAlkDic(alkFin / 50_000.0, inicial.DicMolL);
        }

        if (PhDe(0) >= phObjetivo - 1e-4) return 0;
        var lo = 0.0;
        var hi = 80.0;
        for (var i = 0; i < 56; i++)
        {
            var mid = (lo + hi) / 2;
            if (PhDe(mid) > phObjetivo) hi = mid;
            else lo = mid;
        }

        return (lo + hi) / 2;
    }

    /// <summary>
    /// Sistema cerrado: el CID no cambia; la dosis gasta alcalinidad y la cal la repone.
    /// Aireación: se retira una fracción del H₂CO₃* sin cambiar la alcalinidad (Le Châtelier sobre CO₂(ac) ⇌ CO₂(g)).
    /// Constantes a 25 °C.
    /// </summary>
    public static ResultadoEquilibrio EvaluarEquilibrio(
        double ph0, double alcalinidadMgL, double dosisAlumbreMgL, double dosisCalMgL, double fraccionAireacion)
    {
        var inicial = EstadoCarbonato(ph0, alcalinidadMgL);
        var este = EvaluarEstequiometria(dosisAlumbreMgL, alcalinidadMgL + dosisCalMgL * 100 / MCao, 0);
        var alkFin = alcalinidadMgL + dosisCalMgL * 100 / MCao - AlkPorMgAlumbre * Math.Max(dosisAlumbreMgL, 0);
        var phCerrado = PhDesdeAlkDic(alkFin / 50_000.0, inicial.DicMolL);
        var cerrado = EstadoCarbonato(phCerrado, alkFin);
        var f = Math.Clamp(fraccionAireacion, 0, 1);
        var dicAire = Math.Max(0, inicial.DicMolL * (1 - f * cerrado.Alfa0));
        var phAire = PhDesdeAlkDic(alkFin / 50_000.0, dicAire);
        var aire = EstadoCarbonato(phAire, alkFin);
        var esp0 = FraccionHclo(ph0);
        var espC = FraccionHclo(phCerrado);
        var espA = FraccionHclo(phAire);
        return new ResultadoEquilibrio(
            inicial,
            phCerrado,
            alkFin,
            inicial.DicMolL,
            cerrado.Alfa0,
            cerrado.Alfa1,
            cerrado.Alfa2,
            phAire,
            dicAire,
            aire.Alfa0,
            aire.Alfa1,
            aire.Alfa2,
            esp0.Hclo,
            espC.Hclo,
            espA.Hclo,
            este.Co2MgL,
            AluminioResidual(phCerrado));
    }

    public static IReadOnlyList<Comprobacion> VerificarEntregables()
    {
        var lista = new List<Comprobacion>();
        void Ok(string nombre, bool pasa, string detalle) => lista.Add(new Comprobacion(nombre, pasa, detalle));

        var alk = AlkPorMgAlumbre * 30;
        var lodo = LodoPorMgAlumbre * 30;
        Ok("30 mg/L de alumbre gastan ~15 mg/L de alcalinidad", Math.Abs(alk - 15.1515) < 0.01, $"{alk:0.00} mg/L CaCO3");
        Ok("30 mg/L de alumbre producen ~7,8 mg/L de Al(OH)3", Math.Abs(lodo - 7.879) < 0.02, $"{lodo:0.00} mg/L");

        var cu = EvaluarMolaridad(SolutoPorId("cuso4"), 0.50, 0.30);
        Ok("CuSO4 0,50 mol / 0,30 L está saturado", cu.Saturado && Math.Abs(cu.MolaridadReal - 1.40) < 0.001,
            $"M teórica {cu.MolaridadTeorica:0.00}; M real {cu.MolaridadReal:0.00}");
        var clara = EvaluarMolaridad(SolutoPorId("cuso4"), 0.50, 1.00);
        Ok("CuSO4 0,50 mol / 1,00 L = 0,50 M y no satura", !clara.Saturado && Math.Abs(clara.MolaridadReal - 0.5) < 1e-9,
            $"{clara.MolaridadReal:0.00} M");

        var f7 = FraccionHclo(7);
        var f6 = FraccionHclo(6);
        var f75 = FraccionHclo(7.5);
        var f8 = FraccionHclo(8);
        var f9 = FraccionHclo(9);
        Ok("HClO a pH 7 ≈ 76 %", Math.Abs(f7.Hclo - 0.76) < 0.01, $"{f7.Hclo:P0}");
        Ok("HClO a pH 6 ≈ 97 %", Math.Abs(f6.Hclo - 0.97) < 0.01, $"{f6.Hclo:P0}");
        Ok("HClO a pH 7,5 = 50 %", Math.Abs(f75.Hclo - 0.5) < 1e-9, $"{f75.Hclo:P0}");
        Ok("HClO a pH 8 ≈ 24 %", Math.Abs(f8.Hclo - 0.24) < 0.01, $"{f8.Hclo:P0}");
        Ok("HClO a pH 9 ≈ 3 %", Math.Abs(f9.Hclo - 0.03) < 0.01, $"{f9.Hclo:P0}");

        var hcl = EvaluarAcidoBase("hcl", 0.10);
        var naoh = EvaluarAcidoBase("naoh", 0.10);
        Ok("HCl 0,10 M → pH 1", Math.Abs(hcl.Ph - 1) < 0.02, $"pH {hcl.Ph:0.00}");
        Ok("NaOH 0,10 M → pH 13", Math.Abs(naoh.Ph - 13) < 0.02, $"pH {naoh.Ph:0.00}");

        var v = VolumenGasCloroL(1000, 20, 1);
        Ok("1 kg de Cl2 a 20 °C y 1 atm ≈ 339 L", v > 338 && v < 341, $"{v:0.0} L");
        var pDui = PresionAtm(2590);
        Ok("Duitama, 2590 m ≈ 0,73 atm", pDui > 0.72 && pDui < 0.74, $"{pDui:0.000} atm");
        var pv = PresionVaporCloroBar(20);
        Ok("Presión de vapor del Cl2 a 20 °C ≈ 6,8 bar", pv > 6.6 && pv < 6.95, $"{pv:0.00} bar");

        var gDia = 100 * 1.5 * 86.4;
        var lDia = gDia / CloroDisponible15GL;
        Ok("100 L/s y 1,5 mg/L → 12,96 kg/día de cloro", Math.Abs(gDia - 12960) < 0.1, $"{gDia / 1000:0.00} kg/día");
        Ok("Hipoclorito al 15 % ≈ 91 L/día", lDia > 90 && lDia < 92, $"{lDia:0.0} L/día");

        var co2 = Co2EquilibrioMgL(25, 1);
        Ok("CO2 en equilibrio con el aire ≈ 0,6 mg/L", co2 > 0.5 && co2 < 0.75, $"{co2:0.00} mg/L");
        var o2 = OxigenoSaturadoMgL(25, 1);
        Ok("O2 disuelto a 25 °C cerca de 8 mg/L", o2 > 8.0 && o2 < 8.6, $"{o2:0.00} mg/L");

        var azucar = EvaluarMolaridad(SolutoPorId("sacarosa"), 0.2, 0.5);
        Ok("Azúcar diluida es mezcla homogénea", azucar.Clasificacion.Contains("homogénea", StringComparison.Ordinal), azucar.Clasificacion);
        var sal = EvaluarMolaridad(SolutoPorId("nacl"), 6, 0.5);
        Ok("NaCl por encima de la solubilidad es mezcla heterogénea", sal.Saturado, $"sólido {sal.MolesSolido:0.00} mol");

        var est = EvaluarEstequiometria(55, 20, 100);
        Ok("55 mg/L de alumbre y 20 mg/L de alcalinidad: limita el bicarbonato", est.LimitanteAlcalinidad, est.Limitante);
        Ok("Reaccionan 39,60 mg/L de alumbre y sobran 15,40 mg/L",
            Math.Abs(est.AlumReaccionadoMgL - 39.6) < 0.02 && Math.Abs(est.AlumExcesoMgL - 15.4) < 0.02,
            $"{est.AlumReaccionadoMgL:0.00} y {est.AlumExcesoMgL:0.00} mg/L");
        Ok("Al(OH)3 teórico del limitante = 10,40 mg/L; si reaccionara todo el alumbre, 14,44 mg/L",
            Math.Abs(est.Aloh3TeoricoMgL - 10.4) < 0.02 && Math.Abs(est.Aloh3SiTodoMgL - 14.4444) < 0.02,
            $"{est.Aloh3TeoricoMgL:0.00} / {est.Aloh3SiTodoMgL:0.00} mg/L");
        Ok("Rendimiento de la dosis = 72 %", Math.Abs(est.RendimientoDosis - 0.72) < 0.001, $"{est.RendimientoDosis:P1}");
        Ok("Cal mínima estequiométrica = 4,36 mg/L de CaO", Math.Abs(est.CaoMinimoMgL - 4.3556) < 0.02, $"{est.CaoMinimoMgL:0.00} mg/L");

        var sinCal = EvaluarEquilibrio(7.2, 20, 55, 0, 1);
        Ok("Sin cal, el exceso de alumbre deja el pH cerrado cerca de 3,8", sinCal.PhCerrado > 3.5 && sinCal.PhCerrado < 4.1, $"pH {sinCal.PhCerrado:0.00}");
        Ok("Airear sin cal no recupera el pH: la acidez que sobra no es CO2", Math.Abs(sinCal.PhAireado - sinCal.PhCerrado) < 0.15, $"pH {sinCal.PhAireado:0.00}");
        var soloEsteq = EvaluarEquilibrio(7.2, 20, 55, est.CaoMinimoMgL, 1);
        Ok("Con la cal justa, airear sube el pH de 4,85 a 5,64 y sigue fuera de norma",
            soloEsteq.PhCerrado > 4.6 && soloEsteq.PhCerrado < 5.1 && soloEsteq.PhAireado > 5.4 && soloEsteq.PhAireado < 6.5,
            $"cerrado {soloEsteq.PhCerrado:0.00}; aireado {soloEsteq.PhAireado:0.00}");
        var caoPh7 = CaoParaPh(7, 7.2, 20, 55);
        var conCal = EvaluarEquilibrio(7.2, 20, 55, caoPh7, 0);
        Ok("La cal que devuelve el pH cerrado a 7 hace limitante al alumbre",
            !EvaluarEstequiometria(55, 20 + caoPh7 * 100 / MCao, 100).LimitanteAlcalinidad && Math.Abs(conCal.PhCerrado - 7) < 0.03,
            $"{caoPh7:0.00} mg/L CaO; pH {conCal.PhCerrado:0.00}");
        Ok("Kc del HClO es 3,16e-8", Math.Abs(KcHclo - 3.16227766e-8) / 3.16227766e-8 < 1e-6, $"{KcHclo:0.00E+0}");

        return lista;
    }

    static double Lerp(double a, double b, double t) => a + (b - a) * t;
}

public sealed record Soluto(
    string Id,
    string Formula,
    string Tradicional,
    string Stock,
    string Electrolito,
    double MasaMolar,
    double SolubilidadMolL,
    int Iones);

public sealed record ResultadoMolaridad(
    double MolaridadTeorica,
    double MolaridadReal,
    double MolesDisueltos,
    double MolesSolido,
    double Gramos,
    bool Saturado,
    string Clasificacion,
    double Conductividad);

public sealed record ResultadoAcido(
    double Ph,
    double POH,
    double H,
    double Oh,
    double FraccionDisociada,
    double Conductividad,
    string Titulo,
    string Nota);

public sealed record EntradaPlanta(
    double TurbiedadNtu,
    double AlcalinidadMgL,
    double PhCrudo,
    double CaudalLs,
    double DosisAlumbreMgL,
    double DosisCalMgL,
    double DosisCloroMgL,
    double TemperaturaC,
    double AltitudM);

public sealed record ResultadoPlanta(
    double DemandaAlk,
    double AlcalinidadCal,
    double AlkNeta,
    double BicarbonatoUsado,
    double CalNecesariaMgL,
    double Co2MgL,
    double Co2Equilibrio,
    double LodoMgL,
    double Ph,
    double Aluminio,
    double Sugerida,
    double Remocion,
    double TurbiedadFinal,
    double Hclo,
    double Clo,
    double CloroGDia,
    double HipoLDia,
    double CalorKw,
    double Presion,
    double Oxigeno);

public sealed record Comprobacion(string Nombre, bool Pasa, string Detalle);

public sealed record ResultadoEsteq(
    bool LimitanteAlcalinidad,
    string Limitante,
    string Exceso,
    double DemandaAlkMgL,
    double AlkUsadaMgL,
    double AlkExcesoMgL,
    double AlumReaccionadoMgL,
    double AlumExcesoMgL,
    double NAlumDosisMolL,
    double NAlumReactMolL,
    double NAlkDisponibleMolL,
    double NAloh3MolL,
    double Aloh3TeoricoMgL,
    double Aloh3SiTodoMgL,
    double RendimientoDosis,
    double Co2MgL,
    double CaoMinimoMgL,
    double AlumReaccionadoKgDia,
    double AlumExcesoKgDia,
    double Aloh3KgDia,
    double Co2KgDia,
    double AlumDosadoKgDia);

public sealed record Carbonato(
    double Ph,
    double AlkMgL,
    double AlkEqL,
    double DicMolL,
    double Alfa0,
    double Alfa1,
    double Alfa2,
    double H2co3MolL,
    double Hco3MolL,
    double Co3MolL,
    double H,
    double Oh);

public sealed record ResultadoEquilibrio(
    Carbonato Inicial,
    double PhCerrado,
    double AlkFinMgL,
    double DicCerrado,
    double Alfa0Cerrado,
    double Alfa1Cerrado,
    double Alfa2Cerrado,
    double PhAireado,
    double DicAireado,
    double Alfa0Aire,
    double Alfa1Aire,
    double Alfa2Aire,
    double HcloInicial,
    double HcloCerrado,
    double HcloAireado,
    double Co2ReaccionMgL,
    double AluminioCerrado);
