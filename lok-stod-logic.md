# LOK-stöd – logik för att maximera utbetalning

## Mål

Fördela deltagare och ledare mellan stödberättigade gruppaktiviteter så att LOK-stödet maximeras.

## Grundregel

En stödberättigad grupp behöver minst:

```text
1 ledare
3 deltagare i stödberättigad ålder
```

Ledarstödet är:

```text
1 ledare  = 20 kr per grupp
2+ ledare = 25 kr per grupp
```

Det innebär att en extra ledare i samma grupp bara ökar ledarstödet med 5 kr, medan en separat giltig grupp med 1 ledare kan ge 20 kr.

## Optimeringsregel

När det finns tillräckligt många deltagare bör ledarna därför fördelas över så många giltiga grupper som möjligt.

```text
max_antal_grupper = min(
    antal_ledare,
    floor(antal_deltagare / 3)
)
```

Prioritera:

```text
1 ledare + minst 3 deltagare per grupp
```

innan en andra ledare placeras i en redan giltig grupp.

## Exempel

### 2 ledare + 6 deltagare

Sämre:

```text
Grupp 1:
2 ledare
6 deltagare

Ledarstöd: 25 kr
```

Bättre:

```text
Grupp 1:
1 ledare
3 deltagare

Grupp 2:
1 ledare
3 deltagare

Ledarstöd: 20 + 20 = 40 kr
```

Optimal fördelning:

```text
2 grupper
3 deltagare + 1 ledare per grupp
```

## Algoritm

```python
def maximize_groups(leaders, participants):
    groups = min(leaders, participants // 3)

    if groups == 0:
        return []

    result = [
        {"leaders": 1, "participants": 3}
        for _ in range(groups)
    ]

    remaining_leaders = leaders - groups
    remaining_participants = participants - (groups * 3)

    # Fördela återstående deltagare mellan de giltiga grupperna.
    for i in range(remaining_participants):
        result[i % groups]["participants"] += 1

    # Om alla möjliga giltiga grupper redan skapats kan extra ledare
    # användas som andra ledare och ge +5 kr på respektive grupp.
    for i in range(min(remaining_leaders, groups)):
        result[i]["leaders"] += 1

    return result
```

## Prioriteringsordning

Codex-agenten ska optimera i denna ordning:

```text
1. Skapa maximalt antal grupper med:
   1 ledare + minst 3 deltagare.

2. Fördela återstående deltagare mellan dessa grupper.

3. Om ledare återstår och inga fler giltiga grupper kan skapas:
   placera en extra ledare i befintliga grupper för +5 kr ledarstöd.

4. Undvik 2 ledare i samma grupp om den andra ledaren tillsammans
   med minst 3 deltagare kan bilda en separat giltig grupp.
```

## Kort regel för agenten

```text
Maximera först antalet giltiga grupper.

Varje ny grupp kräver:
- 1 ledare
- minst 3 deltagare

En ny giltig grupp ger 20 kr i ledarstöd.
En andra ledare i en befintlig grupp ger endast ytterligare 5 kr.

Därför ska en ledare användas för att skapa en separat grupp när
det finns minst 3 deltagare tillgängliga för den gruppen.
```

## Viktigt

Detta är endast optimeringslogik för hur ledare och deltagare kan fördelas mellan **faktiskt separata, stödberättigade gruppaktiviteter**. Det ska inte användas för att artificiellt dela upp en och samma aktivitet i fiktiva grupper.
