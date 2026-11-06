using System;

/// <summary>Pruebas ejecutables sin Unity con proyectos y profesores provisionales.</summary>
public static class DeliveryRulesChecks
{
    static int passed;
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        passed++; Console.WriteLine("PASS " + message);
    }
    public static int Main()
    {
        foreach (int count in new[] { 3, 5 })
        {
            var inventory = new ProjectInventory();
            string[] professors = new string[count], projects = new string[count];
            for (int i = 0; i < count; i++) { professors[i] = "prof" + i; projects[i] = "project" + i; inventory.Add(projects[i]); }
            var round = new ProjectDeliverySession(professors, projects, 180, inventory);
            Check(round.DeliveredCount == 0 && round.RequiredCount == count, "Progreso inicial 0/" + count);
            Check(round.TryDeliver(professors[0], projects[1]) == ProjectDeliveryResult.WrongProject, "Rechaza proyecto equivocado");
            Check(inventory.Quantity(projects[1]) == 1 && round.DeliveredCount == 0, "Error no consume ni suma");
            round.SetPaused(true); round.AdvanceTime(60);
            Check(round.RemainingTime == 180 && round.ElapsedTime == 0, "Pausa congela reloj y cronómetro");
            Check(round.TryDeliver(professors[0], projects[0]) == ProjectDeliveryResult.NotPlaying, "No entrega durante pausa");
            round.SetPaused(false); round.AdvanceTime(10);
            Check(round.RemainingTime == 170 && round.ElapsedTime == 10, "Reanudar continúa ambos relojes");
            Check(round.TryDeliver(professors[0], projects[0]) == ProjectDeliveryResult.Accepted, "Acepta el proyecto correcto");
            Check(inventory.Quantity(projects[0]) == 0 && round.DeliveredCount == 1, "Entrega retira proyecto y suma una vez");
            Check(round.TryDeliver(professors[0], projects[0]) == ProjectDeliveryResult.AlreadyDelivered && round.DeliveredCount == 1, "Entrega repetida no cuenta");
            inventory.TryRemove(projects[1]);
            Check(round.TryDeliver(professors[1], projects[1]) == ProjectDeliveryResult.MissingProject && round.DeliveredCount == 1, "No entrega un objeto ausente");
            inventory.Add(projects[1]);
            for (int i = count - 1; i >= 1; i--) Check(round.TryDeliver(professors[i], projects[i]) == ProjectDeliveryResult.Accepted, "Acepta entregas en cualquier orden");
            Check(round.Completed && round.DeliveredCount == count, "Victoria " + count + "/" + count);
            float time = round.RemainingTime; round.AdvanceTime(300);
            Check(round.RemainingTime == time && !round.Failed, "Victoria detiene reloj y evita derrota tardía");
            Check(round.TryDeliver(professors[0], projects[0]) == ProjectDeliveryResult.NotPlaying, "No cambia progreso tras victoria");
        }
        var empty = new ProjectInventory();
        var loss = new ProjectDeliverySession(new[] { "prof" }, new[] { "project" }, 3, empty);
        loss.AdvanceTime(50);
        Check(loss.Failed && loss.RemainingTime == 0 && loss.ElapsedTime == 3, "Tiempo agotado produce derrota sin reloj negativo");
        Check(loss.TryDeliver("prof", "project") == ProjectDeliveryResult.NotPlaying, "No acepta entregas tras derrota");
        var restartInventory = new ProjectInventory(); restartInventory.Add("project");
        var restart = new ProjectDeliverySession(new[] { "prof" }, new[] { "project" }, 180, restartInventory);
        Check(restart.Playing && restart.RemainingTime == 180 && restart.Inventory.Quantity("project") == 1, "Reinicio independiente repone inventario y tiempo");
        Console.WriteLine("YC14_RULES_OK " + passed + " comprobaciones");
        return 0;
    }
}
