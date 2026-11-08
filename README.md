# Simulador PTAP

Laboratorio en C# para la potabilización del agua. Lo escribí porque no hay un simulador que traiga alumbre, cal, hipoclorito y el balance de una PTAP.

Estudiante: Daniel Esteban Ortiz Pinzón. Ingeniería de Sistemas, Química General, Grupo 1. Universidad EAN.

Repositorio: https://github.com/danielorzt/SimuladorPTAP

## Qué calcula

| Banco | Contenido |
| --- | --- |
| Soluciones | Sacarosa, NaCl, CuSO₄, alumbre y NaClO. M = n/V, saturación, mezcla homogénea o heterogénea |
| Ácido-base | Agua, HCl, NaOH, HClO (pKa 7,5) y ClO⁻ |
| Planta | Coagulación, cal, cloro y contraste con la Resolución 2115 de 2007 |
| Gases | Cl₂ con PV = nRT, Antoine (NIST), Henry para CO₂ y Benson–Krause para O₂ |
| Módulo 3 | Reactivo limitante, rendimiento de la dosis y equilibrio del carbonato (Kc, Kp, Le Châtelier) |

La remoción de turbiedad y el aluminio residual son un modelo didáctico. La estequiometría, el gas ideal, Antoine, la especiación, Henry y el equilibrio ácido-base del carbonato son las cuentas de los entregables.

## Cómo ejecutarlo

Requiere el SDK de .NET 8.

```bash
dotnet run --project src/Ptap.Cli
dotnet run --project src/Ptap.Cli -- --caso
```

`--caso` imprime las comprobaciones del avance 2 y el caso del módulo 3, y termina con código 0 si coinciden. La bitácora de cada experimento está en [docs/LABORATORIO.md](docs/LABORATORIO.md).

## Caso del módulo 3

Agua cruda, 100 L/s, pH 7,20, alcalinidad 20 mg/L como CaCO₃, dosis 55 mg/L de Al₂(SO₄)₃·14H₂O.

- Limita la alcalinidad. Reaccionan 39,60 mg/L; sobran 15,40 mg/L.
- Al(OH)₃ teórico: 10,40 mg/L. Si reaccionara toda la dosis: 14,44 mg/L. Rendimiento: 72,0 %.
- Sin cal, el sistema cerrado queda en pH 3,80. Airear no lo sube: la acidez que sobra es H⁺, no CO₂.
- CaO para dejar el pH cerrado en 7: 14,79 mg/L. Ahí el rendimiento pasa a 100 %.
