using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Explorar, deducir, abrir el candado y regresar con la USB.</summary>
public sealed class LevelOneMystery : MonoBehaviour
{
    [SerializeField] Sprite noteSprite;
    [SerializeField] AudioClip returnMusic;
    public LockerMysteryRound Round { get; private set; }
    public bool[] Discovered { get; } = new bool[3];
    public int DiscoveredCount => Discovered.Count(found=>found);
    public int WrongLockers { get; private set; }
    public int WrongCodes { get; private set; }
    public LevelOneMysteryUI UI { get; private set; }
    public LockerInteraction[] TopRow { get; private set; }
    public LockerInteraction[] BottomRow { get; private set; }
    public bool CanUseLock => Round!=null && DiscoveredCount==3 && !manager.HasUSB && !manager.IsFailed && !manager.IsCompleted;
    Level01Manager manager;
    LevelOneUpgrade upgrade;
    PlayerMovement player;
    readonly List<Transform> sources=new List<Transform>();
    readonly List<TMP_Text> sourceLabels=new List<TMP_Text>();
    readonly List<SpriteRenderer> sourceIcons=new List<SpriteRenderer>();
    readonly string[] captions={"Nota de limpieza","Compañero","Aviso del pasillo"};
    LockerInteraction pendingLocker;
    U2GameConfig config;

    public void Configure(Level01Manager levelManager, PlayerMovement actor, LockerInteraction[] lockers)
    {
        manager=levelManager;player=actor;upgrade=GetComponent<LevelOneUpgrade>();
        config=Resources.Load<U2GameConfig>("U2GameConfig");
        TopRow=lockers.Where(l=>l.MysteryRow==0).OrderBy(l=>l.transform.position.x).ToArray();
        BottomRow=lockers.Where(l=>l.MysteryRow==1).OrderBy(l=>l.transform.position.x).ToArray();
        int[] counts=BottomRow.Length>0?new[]{TopRow.Length,BottomRow.Length}:new[]{TopRow.Length};
        if(TopRow.Length==0){Debug.LogError("El misterio necesita casilleros en la fila Azul.",this);return;}
        Round=new LockerMysteryRound(counts,Guid.NewGuid().GetHashCode());
        for(int row=0;row<counts.Length;row++)
        {
            var items=row==0?TopRow:BottomRow;
            for(int i=0;i<items.Length;i++)
            {
                var locker=items[i];locker.InitializeRound(manager,Round.IsTarget(row,i+1));locker.BindMystery(this,i+1);
                AddLockerNumber(locker);
            }
        }
        UI=gameObject.AddComponent<LevelOneMysteryUI>();UI.Configure(this);
        CreateClueSources();
        if(player!=null)player.GetComponent<PlayerInteraction>()?.ConfigureMystery(this);
    }

    void AddLockerNumber(LockerInteraction locker)
    {
        var go=new GameObject("Identificador "+locker.MysteryName);go.transform.SetParent(locker.transform,false);
        go.transform.position=new Vector3(locker.transform.position.x,locker.MysteryRow==0?3.20f:-2.82f,0);
        var label=go.AddComponent<TextMeshPro>();if(LevelOneTypography.Font!=null)label.font=LevelOneTypography.Font;
        label.text=locker.MysteryName;label.fontSize=1.9f;label.alignment=TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta=new Vector2(1.52f,.40f);label.renderer.sortingOrder=8;
        label.color=locker.MysteryRow==0?new Color(.62f,.85f,1):new Color(.60f,1,.72f);
    }

    void CreateClueSources()
    {
        if(player==null)return;
        float min=TopRow.First().InteractionPosition.x, max=TopRow.Last().InteractionPosition.x;
        float bottom=BottomRow.Length>0?BottomRow.Average(l=>l.InteractionPosition.y):-2;
        Vector2[] places={new Vector2(Mathf.Lerp(min,max,.08f),.20f),new Vector2(Mathf.Lerp(min,max,.52f),bottom+.90f),new Vector2(Mathf.Lerp(min,max,.91f),.20f)};
        for(int i=0;i<3;i++)
        {
            var go=new GameObject(captions[i]);go.transform.SetParent(transform,false);go.transform.position=places[i];
            var sprite=go.AddComponent<SpriteRenderer>();sprite.sortingOrder=4;
            if(i==1)
            {
                var actorSprite=player.GetComponent<SpriteRenderer>();sprite.sprite=actorSprite!=null?actorSprite.sprite:null;
                var animator=go.AddComponent<Animator>();
                if(config!=null&&config.studentControllers!=null&&config.studentControllers.Length>0)
                    animator.runtimeAnimatorController=config.studentControllers[config.studentControllers.Length-1];
                animator.speed=0;
            }
            else if(noteSprite!=null)
            {
                sprite.sprite=noteSprite;
                float size=Mathf.Max(.001f,noteSprite.bounds.size.x);go.transform.localScale=Vector3.one*(.72f/size);
            }
            sources.Add(go.transform);sourceIcons.Add(sprite);
            var title=new GameObject("Pista cercana");title.transform.SetParent(transform,false);title.transform.position=places[i]+Vector2.up*.85f;
            var label=title.AddComponent<TextMeshPro>();if(LevelOneTypography.Font!=null)label.font=LevelOneTypography.Font;
            label.text=captions[i];label.fontSize=2.1f;label.alignment=TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta=new Vector2(3,.5f);label.renderer.sortingOrder=10;sourceLabels.Add(label);
        }
    }

    void Update()
    {
        if(Round==null||player==null)return;
        for(int i=0;i<sources.Count;i++)
        {
            bool close=Vector2.Distance(player.transform.position,sources[i].position)<3.5f;
            sourceLabels[i].gameObject.SetActive(close&&!manager.HasUSB&&!manager.IsCompleted&&!manager.IsFailed);
            sourceLabels[i].color=Discovered[i]?new Color(.60f,1,.72f):new Color(1,.85f,.4f);
        }
        if(Keyboard.current!=null&&Keyboard.current.nKey.wasPressedThisFrame&&!manager.IsCompleted&&!manager.IsFailed)
        {
            if(UI.IsOpen) { if(Time.frameCount>openNotebookFrame){if(UI.IsNotebook)UI.Close();else OpenNotebook();} }
            else if(manager.IsPlaying)OpenNotebook();
        }
    }
    int openNotebookFrame;
    public void OpenNotebook(){if(Round==null||manager.IsCompleted||manager.IsFailed)return;openNotebookFrame=Time.frameCount;UI.ShowNotebook();}

    public string NearbyCluePrompt()
    {
        int nearest=FindSource();
        return nearest<0?null:$"{captions[nearest]}\nE / botón sur: "+(Discovered[nearest]?"consultar pista":"investigar");
    }
    int FindSource()
    {
        if(player==null||!manager.IsPlaying)return -1;
        int result=-1;float closest=1.45f;
        for(int i=0;i<sources.Count;i++)
        {float distance=Vector2.Distance(player.transform.position,sources[i].position);if(distance<closest){closest=distance;result=i;}}
        return result;
    }
    public bool TryInteractClue()
    {
        if(manager.HasUSB)return false;
        int index=FindSource();if(index<0)return false;
        if(!Discovered[index]){Discovered[index]=true;upgrade?.FoundClue();manager.ShowFeedback($"Pista guardada · {DiscoveredCount}/3");}
        OpenNotebook();return true;
    }

    public void InvestigateLocker(LockerInteraction locker)
    {
        if(!manager.IsPlaying)return;
        if(manager.HasUSB){manager.ShowFeedback("Ya tienes la USB. Vuelve a la salida.");return;}
        if(Round.IsTarget(locker.MysteryRow,locker.MysteryNumber))
        {
            if(DiscoveredCount<3){manager.ShowFeedback("Tiene un candado. Reúne las tres pistas.");return;}
            pendingLocker=locker;UI.ShowLock();return;
        }
        bool repeat=locker.IsSearched;locker.MarkSearched();WrongLockers++;
        float penalty=repeat?(config!=null?config.repeatLockerPenalty:5):(config!=null?config.wrongLockerPenalty:3);
        upgrade?.Reviewed(repeat,false);manager.ApplyPenalty(penalty);manager.PlayWrongLockerSound();
        manager.ShowFeedback(repeat?$"Ya revisado · −{penalty:0} s":$"Aquí no está · −{penalty:0} s. Consulta tus pistas.");
    }

    public void SubmitCombination(int[] attempt)
    {
        if(!CanUseLock||pendingLocker==null||!UI.IsOpen)return;
        if(!Round.MatchesCombination(attempt))
        {
            WrongCodes++;
            // El candado pausa la lectura; las sanciones todavía se aplican al reloj real del nivel.
            Time.timeScale=1;manager.ApplyPenalty(4);manager.PlayWrongLockerSound();
            if(manager.IsFailed)UI.Close();
            Time.timeScale=0;
            return;
        }
        UI.Close();pendingLocker.MarkSearched();upgrade?.Reviewed(false,true);
        manager.CollectUSB();upgrade?.BeginReturnSequence(returnMusic);
        manager.ShowFeedback("¡Ya la tienes! El conserje está cerrando. Vuelve a la salida.");
    }

    public string Awards(int calm)
    {
        var awards=new List<string>();
        if(DiscoveredCount==3)awards.Add("Detective del pasillo");
        if(WrongLockers==0&&WrongCodes==0)awards.Add("Sin equivocaciones");
        if(calm==3)awards.Add("Sin perder la calma");
        return string.Join(" · ",awards);
    }
}
