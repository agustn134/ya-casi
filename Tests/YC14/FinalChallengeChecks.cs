using System;

/// <summary>Reglas del reto final ejecutables sin escena, física ni reloj de Unity.</summary>
public static class FinalChallengeChecks
{
    public static void Run(Action<bool, string> check)
    {
        if (check == null) throw new ArgumentNullException(nameof(check));

        var state = new FinalDeliveryChallengeState();
        int volley;
        check(!state.Started && !state.Completed && state.RemainingTime == 16f,
            "Reto final espera sin consumir sus dieciséis segundos");
        state.AdvanceTime(50f, true);
        check(state.ElapsedTime == 0f && !state.TryTakeVolley(out volley),
            "El reto no avanza ni lanza avisos antes de iniciarse");
        check(state.Start() && !state.Start(), "El reto final comienza una sola vez");

        state.AdvanceTime(.5f, true);
        check(Close(state.RemainingTime, 15.5f) && !state.TryTakeVolley(out volley),
            "La primera oleada no aparece antes del segundo inicial");
        state.AdvanceTime(30f, false);
        check(Close(state.ElapsedTime, .5f) && !state.TryTakeVolley(out volley),
            "La pausa congela el reto y no emite oleadas");
        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            state.AdvanceTime(invalid, true);
        check(Close(state.ElapsedTime, .5f), "Tiempos inválidos no alteran el reto");

        state.AdvanceTime(.5f, true);
        check(state.TryTakeVolley(out volley) && volley == 0,
            "La primera oleada se anuncia al segundo uno");
        check(!state.TryTakeVolley(out volley) && volley == -1,
            "Cada aviso solo se consume una vez");
        check(!state.Start() && Close(state.ElapsedTime, 1f),
            "Repetir Start no reinicia ni extiende el desafío");
        for (int index = 1; index < FinalDeliveryChallengeState.VolleyCount; index++)
        {
            state.AdvanceTime(FinalDeliveryChallengeState.VolleyIntervalSeconds, true);
            check(state.TryTakeVolley(out volley) && volley == index,
                "Oleada " + (index + 1) + " conserva la cadencia de 2.4 segundos");
        }
        check(Close(state.ElapsedTime, 13f) && !state.Completed,
            "Las seis oleadas terminan de anunciarse al segundo trece");
        check(13f + FinalDeliveryChallengeState.TelegraphSeconds + FinalDeliveryChallengeState.FlightSeconds
            < FinalDeliveryChallengeState.DurationSeconds,
            "El último objeto puede terminar su recorrido antes de habilitar la entrega");
        state.AdvanceTime(2.99f, true);
        check(!state.Completed && !state.TryTakeVolley(out volley),
            "Después de la sexta oleada no se generan objetos adicionales");
        state.AdvanceTime(.01f, true);
        check(state.Completed && state.RemainingTime == 0f && !state.TryTakeVolley(out volley),
            "El reto concluye a los dieciséis segundos sin avisos pendientes");
        state.AdvanceTime(float.MaxValue, true);
        check(state.ElapsedTime == 16f && state.RemainingTime == 0f && !state.Start(),
            "El reto completado no reinicia ni deja tiempo negativo");

        var hitch = new FinalDeliveryChallengeState();
        hitch.Start();
        hitch.AdvanceTime(8.2f, true);
        check(hitch.TryTakeVolley(out volley) && volley == 3 && !hitch.TryTakeVolley(out volley),
            "Un salto de tiempo conserva solo la oleada reciente sin lanzar una ráfaga atrasada");
        hitch.AdvanceTime(.1f, true);
        check(!hitch.TryTakeVolley(out volley), "El siguiente paso no recupera oleadas atrasadas");
        hitch.AdvanceTime(2.3f, true);
        check(hitch.TryTakeVolley(out volley) && volley == 4,
            "La cadencia normal continúa después de un salto de tiempo");

        var stale = new FinalDeliveryChallengeState();
        stale.Start();
        stale.AdvanceTime(1f, true);
        stale.AdvanceTime(.1f, true);
        check(!stale.TryTakeVolley(out volley), "Un aviso no consumido caduca al siguiente paso");
        stale.AdvanceTime(2.3f, true);
        stale.AdvanceTime(1f, false);
        check(!stale.TryTakeVolley(out volley) && Close(stale.ElapsedTime, 3.4f),
            "Al pausar se descarta un aviso pendiente sin avanzar el reloj");

        var completedInOneStep = new FinalDeliveryChallengeState();
        completedInOneStep.Start();
        completedInOneStep.AdvanceTime(100f, true);
        check(completedInOneStep.Completed && completedInOneStep.RemainingTime == 0f
            && !completedInOneStep.TryTakeVolley(out volley),
            "Completar de un solo paso descarta todos los avisos anteriores");
    }

    static bool Close(float left, float right) { return Math.Abs(left - right) < .0001f; }
}
