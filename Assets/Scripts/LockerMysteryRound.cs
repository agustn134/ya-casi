using System;
using System.Collections.Generic;

/// <summary>Una ronda deducible: fila, paridad e intervalo identifican un único casillero.</summary>
public sealed class LockerMysteryRound
{
    public static readonly string[] RowNames = { "Azul", "Verde" };
    public static readonly string[] SymbolNames = { "Sol", "Luna", "Hoja", "Estrella" };

    readonly int[] rowCounts;
    readonly int[] combination;
    readonly string[] clues;
    readonly int lowerNumber;
    readonly int upperNumber;

    /// <summary>Índice de fila desde cero; el número del casillero comienza en uno.</summary>
    public int TargetRow { get; }
    public int TargetNumber { get; }
    public int[] Combination => (int[])combination.Clone();
    public string[] Clues => (string[])clues.Clone();

    public LockerMysteryRound(int[] rowCounts, int seed)
    {
        if (rowCounts == null) throw new ArgumentNullException(nameof(rowCounts));
        if (rowCounts.Length < 1 || rowCounts.Length > RowNames.Length)
            throw new ArgumentException("Debe existir una o dos filas de casilleros.", nameof(rowCounts));
        this.rowCounts = (int[])rowCounts.Clone();
        for (int i = 0; i < rowCounts.Length; i++)
            if (rowCounts[i] < 1)
                throw new ArgumentOutOfRangeException(nameof(rowCounts), "Cada fila necesita al menos un casillero.");

        var random = new Random(seed);
        TargetRow = random.Next(rowCounts.Length);
        int count = rowCounts[TargetRow];
        TargetNumber = random.Next(count) + 1;

        // Un intervalo de dos números permite que el objetivo esté a cualquier lado.
        // El de tres solo es válido cuando la paridad descarta ambos extremos.
        var intervals = new List<int[]>();
        for (int width = Math.Min(2, count); width <= Math.Min(3, count); width++)
        {
            int firstStart = Math.Max(1, TargetNumber - width + 1);
            int lastStart = Math.Min(TargetNumber, count - width + 1);
            for (int start = firstStart; start <= lastStart; start++)
            {
                int end = start + (width - 1);
                int matches = 0;
                for (int offset = 0; offset < width; offset++)
                {
                    int number = start + offset;
                    if (number % 2 == TargetNumber % 2) matches++;
                }
                if (matches == 1) intervals.Add(new[] { start, end });
            }
        }
        int[] interval = intervals[random.Next(intervals.Count)];
        lowerNumber = interval[0];
        upperNumber = interval[1];

        combination = new[] { random.Next(4), random.Next(4), random.Next(4) };
        clues = new[]
        {
            $"La USB está en la fila {RowNames[TargetRow]}.\nCandado: {SymbolNames[combination[0]]} · {SymbolNames[combination[1]]} · {SymbolNames[combination[2]]}.",
            TargetNumber % 2 == 0 ? "Recuerdo que el número del casillero es par." : "Recuerdo que el número del casillero es impar.",
            lowerNumber == upperNumber ? $"El único casillero habilitado es el {lowerNumber}." :
                $"Solo están habilitados los casilleros del {lowerNumber} al {upperNumber}."
        };
    }

    public bool MatchesAllClues(int row, int number)
    {
        return row >= 0 && row < rowCounts.Length && number >= 1 && number <= rowCounts[row] &&
            row == TargetRow && number % 2 == TargetNumber % 2 &&
            number >= lowerNumber && number <= upperNumber;
    }

    public bool IsTarget(int row, int number) => row == TargetRow && number == TargetNumber;

    public bool MatchesCombination(IReadOnlyList<int> attempt)
    {
        if (attempt == null || attempt.Count != combination.Length) return false;
        for (int i = 0; i < combination.Length; i++)
            if (attempt[i] != combination[i]) return false;
        return true;
    }
}
