using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private bool gameOver;
    [SerializeField]
    private ArrayList turnOrder;
    private int comboCount = 0;
    private List<GameObject> enemies;
    private List<GameObject> heroes;

    public static GameManager instance;

    public GameObject activeChar;

    public int enemyTarget;

    void Awake()
    {
        //check if instance exists
        if (instance == null)
        {
            //set instance to this
            instance = this;
        }
        //exists but is another instance
        else if (instance != this)
        {
            //destroy it
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        gameOver = false;

        heroes = GameObject.FindGameObjectsWithTag("Hero").OrderBy(p => p.name).ToArray().ToList();
        enemies = GameObject.FindGameObjectsWithTag("Enemy").OrderBy(p => p.name).ToArray().ToList();
        turnOrder = new ArrayList();

        //build turn order list
        foreach(GameObject hero in heroes)
        {
            turnOrder.Add(hero);
        }
        foreach (GameObject enemy in enemies)
        {
            turnOrder.Add(enemy);
        }
    }

    public void PoseCharacter(string pose)
    {
        string currentPose = activeChar.GetComponent<CharController>().charPose;
        Debug.Log("current: " + currentPose + " new: " + pose);
        //reset pose to neutral when same direction is pressed
        if (currentPose == pose)
        {
            activeChar.GetComponent<CharController>().charPose = "neutral";
        }
        else if (pose == "light" || pose == "heavy")
        {
            string[] poseParts = currentPose.Split("_");
            string attackPose = poseParts[0] + "_" + pose;
            //make it so spamming attacks actually resets to the pose before attacking again
            if (currentPose == attackPose)
            {
                activeChar.GetComponent<CharController>().charPose = poseParts[0];
            }
            else
            {
                activeChar.GetComponent<CharController>().charPose = attackPose;
                if (gameObject.GetComponent<UIManager>().playerTurn)
                {
                    AttackTarget(enemies[enemyTarget]);
                }
            }
        }
        else if (pose != "wait")
        {
            activeChar.GetComponent<CharController>().charPose = pose;
        }

        activeChar.GetComponent<CharController>().UpdatePose();

        if (pose == "wait")
        {
            EndTurn();
        }
    }

    public void StartPlayerTurn()
    {
        enemyTarget = 0;
        gameObject.GetComponent<UIManager>().playerTurn = true;
        gameObject.GetComponent<UIManager>().ToggleContolButtonDisplay();
        enemies[enemyTarget].GetComponent<CharController>().targetSelect.SetActive(true);
        activeChar.GetComponent<CharController>().charPose = "neutral";
        activeChar.GetComponent<CharController>().UpdatePose();

    }

    public void ChangeTargetSelection(string direction)
    {
        enemies[enemyTarget].GetComponent<CharController>().targetSelect.SetActive(false);

        enemyTarget = (direction == "left") ? enemyTarget - 1 : enemyTarget + 1;

        //wrap around target selection
        if (enemyTarget >= enemies.Count)
        {
            enemyTarget = 0;
        }
        else if (enemyTarget < 0)
        {
            enemyTarget = enemies.Count - 1;
        }

        enemies[enemyTarget].GetComponent<CharController>().targetSelect.SetActive(true);
    }

    public void HideAllTargetSelections()
    {
        foreach (GameObject enemy in enemies)
        {
            enemy.GetComponent<CharController>().targetSelect.SetActive(false);
        }
    }

    public void AttackTarget(GameObject targetObject)
    {
        string targetName = targetObject.name;
        string enemyObjectIndex = targetName.Replace("ENEMY ", "");

        //reset current defense to base defense before attacking
        int baseDefense = activeChar.GetComponent<CharController>().charDefenseBase;
        activeChar.GetComponent<CharController>().charDefenseCurrent = baseDefense;

        //check attack strength and speed values
        int strength = activeChar.GetComponent<CharController>().charStrengthCurrent;
        string pose = activeChar.GetComponent<CharController>().charPose;
        int attackStrength = gameObject.GetComponent<Attack>().damageCalc(strength, pose);
        int attackCost = gameObject.GetComponent<Attack>().selectedAttackCost;

        //reduce timer value
        activeChar.GetComponent<CharController>().charSpeedCurrent -= attackCost;
        int newCurrentSpeed = activeChar.GetComponent<CharController>().charSpeedCurrent;
        gameObject.GetComponent<UIManager>().timerText.text = newCurrentSpeed.ToString();

        //update combo text
        comboCount++;
        gameObject.GetComponent<UIManager>().heroComboText.text = comboCount + " HITS";
        if (comboCount > 1)
        {
            gameObject.GetComponent<UIManager>().heroComboText.GetComponent<Animation>().Stop();
            gameObject.GetComponent<UIManager>().heroComboText.GetComponent<Animation>().Play();
        }
        else
        {
            gameObject.GetComponent<UIManager>().heroComboText.text = "";
        }

        //reduce defense if more time was used than available
        if (newCurrentSpeed < 0)
        {
            activeChar.GetComponent<CharController>().charDefenseCurrent = activeChar.GetComponent<CharController>().charDefenseCurrent  + newCurrentSpeed;

            if (activeChar.GetComponent<CharController>().charDefenseCurrent < 0)
            {
                activeChar.GetComponent<CharController>().charDefenseCurrent = 0;
            }
        }

        //deal damage
        CharController target = targetObject.GetComponent<CharController>();
        int damageToTake = attackStrength - target.charDefenseCurrent;
        if (damageToTake < 1) { damageToTake = 1; }
        target.TakeDamage(damageToTake);

        //subract 1 from enemy index since the named index starts at 1
        int enemyIndex = int.Parse(enemyObjectIndex) - 1;
        GameObject damageText = gameObject.GetComponent<UIManager>().enemyDamageText[enemyIndex];
        damageText.GetComponent<TMP_Text>().text = damageToTake.ToString();
        damageText.GetComponent<Animation>().Play();

        PrepNextTurn();

        if (!target.charAlive)
        {
            target.charPose = "ko";
            target.UpdatePose();
            target.targetSelect.SetActive(false);
            turnOrder.Remove(targetObject);
            enemies.Remove(targetObject);

            //check remaining characters for win/lose state
            CheckGameOver();
        }

        CheckRemainingSpeed(newCurrentSpeed);
    }

    public void SkillTarget(GameObject targetObject)
    {
        //reset current defense to base defense before attacking
        int baseDefense = activeChar.GetComponent<CharController>().charDefenseBase;
        activeChar.GetComponent<CharController>().charDefenseCurrent = baseDefense;

        //check attack strength and speed values
        int strength = activeChar.GetComponent<CharController>().charStrengthCurrent;
        string pose = activeChar.GetComponent<CharController>().charPose;
        int skillStrength = gameObject.GetComponent<Skill>().damageCalc(strength, pose);
        int skillCost = gameObject.GetComponent<Skill>().selectedSkillCost;

        //reduce timer value
        activeChar.GetComponent<CharController>().charSpeedCurrent -= skillCost;
        int newCurrentSpeed = activeChar.GetComponent<CharController>().charSpeedCurrent;
        gameObject.GetComponent<UIManager>().timerText.text = newCurrentSpeed.ToString();

        //reduce defense if more time was used than available
        if (newCurrentSpeed < 0)
        {
            activeChar.GetComponent<CharController>().charDefenseCurrent = activeChar.GetComponent<CharController>().charDefenseCurrent + newCurrentSpeed;

            if (activeChar.GetComponent<CharController>().charDefenseCurrent < 0)
            {
                activeChar.GetComponent<CharController>().charDefenseCurrent = 0;
            }
        }

        //loop to deal with multi target skills if needed
        foreach (GameObject enemy in enemies)
        {
            //deal damage
            CharController target = enemy.GetComponent<CharController>();
            int damageToTake = skillStrength - target.charDefenseCurrent;
            if (damageToTake < 1) { damageToTake = 1; }
            target.TakeDamage(damageToTake);

            //subract 1 from enemy index since the named index starts at 1
            int enemyIndex = int.Parse(enemy.name) - 1;
            GameObject damageText = gameObject.GetComponent<UIManager>().enemyDamageText[enemyIndex];
            damageText.GetComponent<TMP_Text>().text = damageToTake.ToString();
            damageText.GetComponent<Animation>().Play();

            if (!target.charAlive)
            {
                target.charPose = "ko";
                target.UpdatePose();
                target.targetSelect.SetActive(false);
                turnOrder.Remove(enemy);
                enemies.Remove(enemy);

                //check remaining characters for win/lose state
                CheckGameOver();
            }
        }

        PrepNextTurn();

        CheckRemainingSpeed(newCurrentSpeed);
    }

    public void Defend()
    {
        //modify def stat
        int baseDefense = activeChar.GetComponent<CharController>().charDefenseBase;
        activeChar.GetComponent<CharController>().charDefenseCurrent = baseDefense * 2;

        //reset speed stat
        activeChar.GetComponent<CharController>().charSpeedCurrent = activeChar.GetComponent<CharController>().charSpeedBase;

        EndTurn();
    }

    private void PrepNextTurn()
    {
        //update char status display
        activeChar.GetComponent<CharController>().UpdateStatus();
    }

    private void CheckRemainingSpeed(int newCurrentSpeed)
    {
        if (newCurrentSpeed <= 0)
        {
            //change sprite to idle pose if they were attacking, denoted by an underscore in the pose name
            if (activeChar.GetComponent<CharController>().charPose.Contains("_"))
            {
                activeChar.GetComponent<CharController>().charPose = "neutral";
                activeChar.GetComponent<CharController>().UpdatePose();
            }

            //reset speed stat
            activeChar.GetComponent<CharController>().charSpeedCurrent = activeChar.GetComponent<CharController>().charSpeedBase;
            EndTurn();
        }
    }

    public void StartEnemyTurn()
    {
        gameObject.GetComponent<UIManager>().playerTurn = false;
        gameObject.GetComponent<UIManager>().ToggleContolButtonDisplay();
        HideAllTargetSelections();
        activeChar.GetComponent<CharController>().charPose = "neutral";
        activeChar.GetComponent<CharController>().UpdatePose();
        StartCoroutine(EnemyActions());
    }

    private IEnumerator EnemyActions()
    {
        int randomNumber = Random.Range(1, 11);
        //even random number (1-10) for movement, odd for action
        if (randomNumber % 2 == 0)
        {
            //TODO: movement
             
        }
        else
        {
            //TODO: action
        }

        //delay so player can see what happened
        yield return new WaitForSeconds(2);

        //random player target
        int heroIndex = Random.Range(0, heroes.Count);
        GameObject targetObject = heroes[heroIndex];
        CharController target = targetObject.GetComponent<CharController>();

        int enemyStrength = activeChar.GetComponent<CharController>().charStrengthCurrent;
        string pose = activeChar.GetComponent<CharController>().charPose;
        int attackStrength = gameObject.GetComponent<Attack>().damageCalc(enemyStrength, pose);
        int damageToTake = attackStrength - target.charDefenseCurrent;
        if (damageToTake < 1) { damageToTake = 1; }

        target.TakeDamage(damageToTake);

        GameObject damageText = gameObject.GetComponent<UIManager>().heroDamageText[heroIndex];
        damageText.GetComponent<TMP_Text>().text = damageToTake.ToString();
        damageText.GetComponent<Animation>().Play();

        //change sprite to idle pose
        activeChar.GetComponent<CharController>().charPose = "neutral";
        activeChar.GetComponent<CharController>().UpdatePose();

        if (!target.charAlive)
        {
            target.charPose = "ko";
            target.UpdatePose();
            turnOrder.Remove(targetObject);
            heroes.Remove(targetObject);

            //check remaining characters for win/lose state
            CheckGameOver();
        }

        EndTurn();
    }

    public void EndTurn()
    {
        comboCount = 0;

        //update char status display
        activeChar.GetComponent<CharController>().UpdateStatus();

        if (gameOver) { return; }

        //clear menu description text
        gameObject.GetComponent<UIManager>().menuText.text = "";
        gameObject.GetComponent<UIManager>().timerText.text = "";

        //end active char turn
        activeChar = (GameObject)turnOrder[0];
        if (activeChar != null && activeChar.GetComponent<CharController>().charAlive)
        {
            activeChar.GetComponent<CharController>().activeTurn = false;
            activeChar.GetComponent<CharController>().CheckTurn();
        }

        //move active char to back of turn order list
        turnOrder.RemoveAt(0);
        turnOrder.Add(activeChar);
        NextTurn();
    }

    public void NextTurn()
    {
        activeChar = (GameObject)turnOrder[0];
        if (activeChar != null && activeChar.GetComponent<CharController>().charAlive)
        {
            activeChar.GetComponent<CharController>().activeTurn = true;
            activeChar.GetComponent<CharController>().CheckTurn();

            if (turnOrder.Count > 1)
            {
                GameObject nextCharObject = (GameObject)turnOrder[1];

                string nextChar = nextCharObject.GetComponent<CharController>().charName;
                gameObject.GetComponent<UIManager>().nextTurnText.text = nextChar;

                //turn next char text red for enemies as an extra visual indicator
                if (nextCharObject.tag == "Enemy")
                {
                    gameObject.GetComponent<UIManager>().nextTurnText.color = Color.red;
                }
                else
                {
                    gameObject.GetComponent<UIManager>().nextTurnText.color = Color.black;
                }
            }
            else
            {
                gameObject.GetComponent<UIManager>().nextTurnText.text = "";
            }
        }
        else
        {
            turnOrder.RemoveAt(0);
            EndTurn();
        }
    }

    private void CheckGameOver()
    {
        if (heroes.Count == 0)
        {
            gameOver = true;
            gameObject.GetComponent<UIManager>().gameOverText.text = "YOU LOSE";
        }
        else if (enemies.Count == 0)
        {
            gameOver = true;
            gameObject.GetComponent<UIManager>().gameOverText.text = "YOU WIN";
        }
        if (enemies.Count > 0)
        {
            enemies[0].GetComponent<CharController>().targetSelect.SetActive(true);
        }
        gameObject.GetComponent<UIManager>().ShowGameOverScreen(gameOver);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
