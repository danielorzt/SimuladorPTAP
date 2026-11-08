# Bitácora — SimuladorPTAP

Daniel Esteban Ortiz Pinzón. Ingeniería de Sistemas, Grupo 1, Química General, Universidad EAN.
Proceso: potabilización del agua. El código que reproduce estas corridas está en `src/Ptap.Core/Motor.cs`.
Comando de cierre: `dotnet run --project src/Ptap.Cli -- --caso`

Constantes usadas en todo el laboratorio: alumbre comercial Al₂(SO₄)₃·14H₂O, 594 g/mol; Al(OH)₃, 78 g/mol; CO₂, 44 g/mol; CaO, 56 g/mol; Cl₂, 70,9 g/mol; R = 0,082057 L·atm/(mol·K).

## Avance 2 — soluciones, ácido-base, planta y gases

### Soluciones (CuSO₄, solubilidad 1,40 mol/L)

| Corrida | n (mol) | V (L) | M = n/V | M en solución | Estado |
| --- | ---: | ---: | ---: | ---: | --- |
| 1 | 0,50 | 1,00 | 0,50 | 0,50 | Homogénea |
| 2 | 0,50 | 0,50 | 1,00 | 1,00 | Homogénea |
| 3 | 0,25 | 0,50 | 0,50 | 0,50 | Igual concentración que la 1 |
| 4 | 0,50 | 0,30 | 1,67 | 1,40 | Saturada, sólido en el fondo |

Sacarosa 0,20 mol en 0,50 L: mezcla homogénea, no electrolito. NaCl 6 mol en 0,50 L: heterogénea, queda sólido.

### Ácido-base

| Sistema | C (mol/L) | pH |
| --- | ---: | ---: |
| HCl | 0,10 | 1,00 |
| NaOH | 0,10 | 13,00 |
| HClO | — | pKa 7,5 |

Especiación del cloro libre, Kc = 3,16×10⁻⁸:

| pH | HClO |
| ---: | ---: |
| 6,0 | 97 % |
| 7,0 | 76 % |
| 7,5 | 50 % |
| 8,0 | 24 % |
| 9,0 | 3 % |

### Planta, caso de 30 mg/L

30 mg/L de alumbre gastan 15,15 mg/L de alcalinidad como CaCO₃ y producen 7,88 mg/L de Al(OH)₃. Caudal 100 L/s y 1,5 mg/L de cloro: 12,96 kg/día; hipoclorito al 15 % m/v (143 g/L de cloro disponible): 90,6 L/día.

### Gases

| Magnitud | Resultado |
| --- | --- |
| 1 kg de Cl₂ a 20 °C y 1 atm | 339,3 L |
| Duitama, 2 590 m | 0,729 atm |
| El mismo kilo a 15 °C y 0,729 atm | cerca de 458 L |
| Presión de vapor del Cl₂ a 20 °C (Antoine, NIST) | 6,73 bar |
| CO₂ en equilibrio con el aire, 25 °C y 1 atm | 0,63 mg/L |
| O₂, Benson–Krause, 25 °C y 1 atm | 8,26 mg/L |

## Módulo 3 — caso de estequiometría y equilibrio

Agua cruda: caudal 100 L/s, pH 7,20, alcalinidad 20,0 mg/L como CaCO₃, dosis 55,0 mg/L de alumbre. Reacción:

Al₂(SO₄)₃·14H₂O + 3 Ca(HCO₃)₂ → 2 Al(OH)₃ + 3 CaSO₄ + 6 CO₂ + 14 H₂O

| Magnitud | Valor |
| --- | --- |
| n dosificado | 9,259×10⁻⁵ mol/L (0,09259 mol/m³) |
| n de alcalinidad | 2,000×10⁻⁴ mol/L |
| n que reacciona | 6,667×10⁻⁵ mol/L |
| Reactivo limitante | Alcalinidad |
| Exceso de alumbre | 15,40 mg/L (133,06 kg/día) |
| Al(OH)₃ del limitante | 10,40 mg/L (89,86 kg/día) |
| Al(OH)₃ si reaccionara toda la dosis | 14,44 mg/L |
| Rendimiento de la dosis | 72,0 % |
| CO₂ producido | 17,60 mg/L (152,06 kg/día) |
| Alumbre dosificado | 475,20 kg/día |
| CaO mínimo (solo estequiometría) | 4,36 mg/L |
| CaO para pH cerrado = 7 | 14,79 mg/L (127,82 kg/día) |

Equilibrio a 25 °C. CID inicial = 4,560×10⁻⁴ mol/L. Kc₁ = 4,47×10⁻⁷. Kc₂ = 4,68×10⁻¹¹. Kp(CO₂) = 29,41 atm·L/mol.

| Corrida | Variable | pH | Qué se observa |
| --- | --- | ---: | --- |
| Inicial | — | 7,20 | α(H₂CO₃*) 12,4 %; α(HCO₃⁻) 87,6 %; HClO 66,6 % |
| Cerrado, sin cal | se consume el HCO₃⁻ | 3,80 | α0 = 99,7 %; HClO ≈ 100 %; Al residual del modelo 1,11 mg/L |
| Aireado, sin cal | se retira el H₂CO₃* | 3,81 | El pH no vuelve: la acidez que sobra es H⁺, no CO₂ |
| Cal justa, 4,36 mg/L, cerrado | alcalinidad final = 0 | 4,85 | El rendimiento estequiométrico ya es 100 % y el pH sigue ácido |
| Esa cal, aireado | se retira el H₂CO₃* | 5,64 | Sube y permanece bajo 6,5 |
| CaO 14,79 mg/L, cerrado | objetivo pH 7 | 7,00 | Limita el alumbre; Al(OH)₃ 14,44 mg/L; HClO 76 % |

La aireación aplica Le Châtelier a CO₂(ac) ⇌ CO₂(g). La cal aplica Le Châtelier al carbonato porque aporta OH⁻ y consume H⁺. El aluminio residual y la remoción de turbiedad siguen siendo un modelo didáctico; no reemplazan un ensayo de jarras.
