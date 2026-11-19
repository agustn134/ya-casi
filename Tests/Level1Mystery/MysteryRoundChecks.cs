using System;
using System.Linq;
using System.Text.RegularExpressions;

public static class MysteryRoundChecks
{
    static int checks;
    static void Require(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }

    public static int Main()
    {
        int[][] configurations = { new[] { 1 }, new[] { 2 }, new[] { 3 }, new[] { 20 },
            new[] { 1, 1 }, new[] { 1, 30 }, new[] { 29, 1 }, new[] { 20, 20 }, new[] { 48, 37 } };
        foreach (int[] counts in configurations)
        for (int seed = -1000; seed < 1000; seed++)
        {
            var round = new LockerMysteryRound(counts, seed);
            string[] clues = round.Clues;
            Require(clues.Length == 3, "Deben existir tres pistas.");
            Require(clues[0].Contains(LockerMysteryRound.RowNames[round.TargetRow]), "La nota identifica una fila incorrecta.");
            bool even = !clues[1].Contains("impar");
            int[] range = Regex.Matches(clues[2], @"\d+").Cast<Match>().Select(m => int.Parse(m.Value)).ToArray();
            int low = range[0], high = range.Length == 1 ? low : range[1];
            int matching = 0;
            for (int row = 0; row < counts.Length; row++)
            for (int number = 1; number <= counts[row]; number++)
            {
                bool inferredFromText = row == round.TargetRow && ((number % 2 == 0) == even) && number >= low && number <= high;
                Require(inferredFromText == round.MatchesAllClues(row, number), "Las pistas escritas no coinciden con la deducción.");
                if (inferredFromText)
                {
                    matching++;
                    Require(round.IsTarget(row, number), "Las pistas señalan un casillero sin USB.");
                }
            }
            Require(matching == 1, "La ronda tiene cero o varias soluciones.");
            Require(!round.MatchesAllClues(-1, 1) && !round.MatchesAllClues(0, 0), "Se aceptó una coordenada inválida.");
            Require(round.MatchesCombination(round.Combination), "Se rechazó el código correcto.");
            for (int i = 0; i < 3; i++)
            {
                int[] wrong = round.Combination;
                wrong[i] = (wrong[i] + 1) % 4;
                Require(!round.MatchesCombination(wrong), "Se aceptó un símbolo incorrecto.");
            }
            Require(!round.MatchesCombination(null) && !round.MatchesCombination(new[] { 0, 1 }), "Se aceptó un código incompleto.");
            Require(round.Combination.All(v => v >= 0 && v < 4), "Símbolo fuera del catálogo.");
            var replay = new LockerMysteryRound(counts, seed);
            Require(round.TargetRow == replay.TargetRow && round.TargetNumber == replay.TargetNumber && round.Combination.SequenceEqual(replay.Combination), "La semilla no reproduce la ronda.");
        }
        foreach (int[] bad in new[] { Array.Empty<int>(), new[] { 0 }, new[] { 1, -2 }, new[] { 1, 1, 1 } })
        {
            bool rejected = false;
            try { new LockerMysteryRound(bad, 0); } catch (ArgumentException) { rejected = true; }
            Require(rejected, "Se aceptó una configuración imposible.");
        }
        Console.WriteLine($"Correcto: 18 000 rondas, {checks:N0} comprobaciones de deducción y candado.");
        return 0;
    }
}
