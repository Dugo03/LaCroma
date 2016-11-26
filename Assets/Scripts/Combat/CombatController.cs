using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

public class CombatController : MonoBehaviour {

    public int turnCounter;

    #region Character Lists
    public List<GameObject> charList;
    public List<GameObject> enemyList;
    public List<GameObject> playerList;

    public List<GameObject> deadCharList;
    public List<GameObject> deadEnemyList;
    public List<GameObject> deadPlayerList;
    #endregion

    #region Action Lists
    public List<Acao> earlyActions;
    public List<Acao> standardActions;
    public List<Acao> lateActions;
    #endregion
    private PortraitController portraitController;

    #region Current State
    private GameObject currentChar;
    private Acao currentAction;
    private Stack<GameObject> currentTurn;
    private List<Acao> acoesTurno;

    private CombatStarter starter;
    public CombatStarter Starter { set { starter = value; } }

    private bool gameOverScreen = false;
    #endregion

    void Start () {

        portraitController = GameObject.Find("OrdemTurno").GetComponent<PortraitController>();
        portraitController.Controller = this;
        portraitController.newTurn();

        enemyList = new List<GameObject>(GameObject.FindGameObjectsWithTag("EnemyParty"));
        playerList = new List<GameObject>(GameObject.FindGameObjectsWithTag("PlayerParty"));

        enemyList.Sort(new HierarchyComparer());
        enemyList.Reverse();
        playerList.Sort(new HierarchyComparer());
        playerList.Reverse();

        charList = new List<GameObject>(playerList);
        charList.AddRange(enemyList);

        deadCharList = new List<GameObject>();
        deadEnemyList = new List<GameObject>();
        deadPlayerList = new List<GameObject>();

        charList = SortSpeed(charList);
        charList.Reverse();
        currentTurn = new Stack<GameObject>();
        for (int i = 0; i < charList.Count; i++)
        {
            charList[i].GetComponent<CombatCharacter>().Priority = charList.Count - 1 - i;
        }

        currentTurn = new Stack<GameObject>(charList);

        turnCounter = 0;

        positionChars();

        GameObject.Find("IrisCombat").GetComponent<CombatCharacter>().currentHP = GameObject.Find("Iris").GetComponent<Character>().currentHP;

        foreach (GameObject character in charList)
        {
            character.GetComponent<CombatCharacter>().OnDie += removeFromCombat;
            character.GetComponent<CombatCharacter>().setSaturation();
            character.BroadcastMessage("currentLife", (float)character.GetComponent<CombatCharacter>().currentHP / character.GetComponent<CombatCharacter>().maxHP);
        }
    }

    private void removeFromCombat(object sender, EventArgs e)
    {
        GameObject character = ((CombatCharacter)sender).gameObject;
        charList.Remove(character);
        deadCharList.Add(character);
        Acao acaoTurno = character.GetComponentInChildren<Acao>();
        acoesTurno.Remove(acaoTurno);
        if (character.tag == "PlayerParty")
        {
            playerList.Remove(character);
            deadPlayerList.Add(character);
        }
        else
        {
            enemyList.Remove(character);
            deadEnemyList.Add(character);
        }
    }

    private List<GameObject> SortSpeed(List<GameObject> sort)
    {
        if (sort.Count > 0)
        {
            System.Random rand = new System.Random();
            int pivot = rand.Next(0, sort.Count);
            CombatCharacter pivotCharacter = sort[pivot].GetComponent<CombatCharacter>();
            List<GameObject> smaller = new List<GameObject>();
            List<GameObject> greater = new List<GameObject>();
            for (int i = 0; i < sort.Count; i++)
            {
                CombatCharacter iCharacter = sort[i].GetComponent<CombatCharacter>();
                if (i != pivot)
                {
                    if (pivotCharacter.Speed > iCharacter.Speed)
                    {
                        smaller.Add(sort[i]);
                    }
                    else if (pivotCharacter.Speed < iCharacter.Speed)
                    {
                        greater.Add(sort[i]);
                    }
                    else
                    {
                        if (rand.Next(0, 2) < 1)
                        {
                            smaller.Add(sort[i]);
                        }
                        else
                        {
                            greater.Add(sort[i]);
                        }
                    }
                }
            }

            smaller = SortSpeed(smaller);
            greater = SortSpeed(greater);

            smaller.Add(sort[pivot]);
            smaller.AddRange(greater);
            return smaller;
        }
        else
            return sort;
    }

    void Update ()
    {
        #region Get Actions
        if (currentChar != null)
        {
            Acao nextAction = currentChar.GetComponent<CombatCharacterController>().getAction();
            if (nextAction != null)
            {
                if (currentTurn.Count != 0)
                {
                    portraitController.refreshPortraits();
                    currentChar = currentTurn.Pop();
                    //Debug.Log(currentChar + " Now playing");
                }
                else
                {
                    portraitController.refreshPortraits();
                    earlyActions.AddRange(standardActions);
                    earlyActions.AddRange(lateActions);
                    earlyActions.Reverse();

                    acoesTurno = new List<Acao>(earlyActions);
                    currentChar = null;
                    currentAction = getNextAction();
                }
            }
        }
        else if (currentTurn.Count != 0)
        {
            currentChar = currentTurn.Pop();
            //Debug.Log(currentChar + " Now playing");
        }
        #endregion
        #region Execute Actions
        else
        {
            if (currentAction != null)
            {
                if (!currentAction.Started)
                    currentAction.execute();
                else if (currentAction.Ended)
                {
                    if (playerList.Count == 0 && !gameOverScreen)
                    {
                        gameOver();
                    }
                    else if (enemyList.Count == 0)
                        endCombat(true);
                    else if (acoesTurno.Count != 0)
                        currentAction = getNextAction();
                    else if (!gameOverScreen)
                        currentAction = null;
                }
            }
            #endregion
            else
                startNextTurn();
        }
    }

    private void startNextTurn()
    {
        /*It's important to sort the list before removing modifiers, 
         *otherwise priority modifiers wouldn't take effect*/
        charList.Sort(new CharacterPriorityComparer());
        charList.Reverse();
        currentTurn = new Stack<GameObject>(charList);

        foreach (GameObject obj in charList)
        {
            obj.GetComponent<CombatCharacterController>().resetAction();
            obj.GetComponent<Character>().nextTurn();
            StatModifier[] charModifiers = obj.GetComponents<StatModifier>();
            foreach (StatModifier modifier in charModifiers)
            {
                modifier.endTurn();
            }
        }
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Action"))
        {
            Destroy(obj);
        }

        earlyActions = new List<Acao>();
        standardActions = new List<Acao>();
        lateActions = new List<Acao>();

        portraitController.newTurn();

        turnCounter++;

    }

    private void endCombat(bool won)
    {
        //Parar de receber Inputs de combate.
        //Mostrar tela de prêmios ao jogador.
        //Animações de Vitória
        GameObject.FindGameObjectWithTag("SceneChanger").BroadcastMessage("enterCombat", false);
        Time.timeScale = 1;
        //GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>().enabled = true;
        //GameObject.FindGameObjectWithTag("CombatCamera").GetComponent<Camera>().enabled = false;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("EnemyParty");
        foreach (GameObject enemy in enemies)
            Destroy(enemy);
        GameObject[] playerParty = GameObject.FindGameObjectsWithTag("PlayerParty");
        foreach (GameObject playerChar in playerParty)
        {
            StatModifier[] modifiers = playerChar.GetComponents<StatModifier>();
            foreach (StatModifier modifier in modifiers)
                Destroy(modifier);
        }
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Action"))
        {
            Destroy(obj);
        }

        if (won)
            Destroy(starter.gameObject);

        GameObject iris = GameObject.Find("Iris");

        if (GameObject.Find("IrisCombat").GetComponent<CombatCharacter>().currentHP > 0)
            iris.GetComponent<Character>().currentHP = GameObject.Find("IrisCombat").GetComponent<CombatCharacter>().currentHP;
        else
            iris.GetComponent<Character>().currentHP = 1;

        float percentage = (float)iris.GetComponent<Character>().currentHP / iris.GetComponent<Character>().maxHP;

        AnimateArgs animateArgs = new AnimateArgs("CurrentLife");
        animateArgs.F = percentage;

        iris.BroadcastMessage("animateFloat", animateArgs);
        iris.BroadcastMessage("setSaturation", percentage);


        Destroy(gameObject);
    }

    private void gameOver()
    {
        Time.timeScale = 1;
        GameObject.Find("Game_Over").BroadcastMessage("gameOver");
        GameObject.Find("Combat_Camera").SendMessage("fade");
        gameOverScreen = true;   
    }

    private Acao getNextAction()
    {
        Acao result = acoesTurno[acoesTurno.Count - 1];
        acoesTurno.RemoveAt(acoesTurno.Count - 1);
        return result;
    }

    private void positionChars()
    {
        playerList[0].gameObject.transform.localPosition = new Vector2(-2.23f, 0.77f);
        playerList[0].gameObject.GetComponent<CombatCharacter>().setSortingLayer("CombatMiddle");
        enemyList[0].gameObject.transform.localPosition = new Vector2(3.16f, 0.33f);
        enemyList[0].gameObject.GetComponent<CombatCharacter>().setSortingLayer("CombatMiddle");
        if (playerList.Count > 1)
        {
            playerList[1].gameObject.transform.localPosition = new Vector2(-3.29f, 1.28f);
            playerList[1].gameObject.GetComponent<CombatCharacter>().setSortingLayer("CombatBack");
        }
        if (enemyList.Count > 1)
        {
            enemyList[1].gameObject.transform.localPosition = new Vector2(4.45f, 1.34f);
            enemyList[1].gameObject.GetComponent<CombatCharacter>().setSortingLayer("CombatBack");
        }
        if (playerList.Count > 2)
        {
            playerList[2].gameObject.transform.localPosition = new Vector2(-4.04f, 0.17f);
            playerList[2].gameObject.GetComponent<CombatCharacter>().setSortingLayer("CombatFront");
        }
        if (enemyList.Count > 2)
        {
            enemyList[2].gameObject.transform.localPosition = new Vector2(5.05f, -0.18f);
            enemyList[2].gameObject.GetComponent<CombatCharacter>().setSortingLayer("CombatFront");
        }
    }
}


