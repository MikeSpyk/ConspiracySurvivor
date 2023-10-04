using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCBattleManager : MonoBehaviour
{
    [SerializeField] private GameObject m_NPCBattleFieldPrefab;
    [SerializeField] private GameObject m_NPCBattleTrainerPrefab;
    [SerializeField] private int m_battleFieldSize = 100;
    [SerializeField] private bool DEBUG_restartBattle = false;

    private bool m_isActive = false;
    private Vector3 m_battleFieldOrigin = Vector3.zero;
    private NPCBattleField m_NPCBattleField = null;
    private NPCBattleTrainer m_NPCBattleTrainer = null;

    private void Awake()
    {
        GameObject battlefield = Instantiate(m_NPCBattleFieldPrefab);
        battlefield.transform.parent = transform;
        m_NPCBattleField = battlefield.GetComponent<NPCBattleField>();
        m_NPCBattleField.setNPCBattleManager(this);
        m_NPCBattleField.setBattleFieldSize(m_battleFieldSize);

        GameObject trainer = Instantiate(m_NPCBattleTrainerPrefab);
        trainer.transform.parent = transform;
        m_NPCBattleTrainer = trainer.GetComponent<NPCBattleTrainer>();
        m_NPCBattleTrainer.setBattlefieldSize(m_battleFieldSize);
        m_NPCBattleTrainer.setBattleManager(this);
    }

    private void Update()
    {
        if(DEBUG_restartBattle)
        {
            if(m_isActive)
            {
                m_NPCBattleTrainer.startBattle(m_battleFieldOrigin);
                m_isActive = true;
            }

            DEBUG_restartBattle = false;
        }
    }

    public bool active { get { return m_isActive; } }

    public void attackPlayer(Player_base player)
    {
        m_isActive = true;
        m_battleFieldOrigin = player.transform.position;
        NPCManager.singleton.createBattlefieldRequest(this);
    }

    public void startCreateBattlefield() // called from NPCManager
    {
        m_NPCBattleTrainer.cancelCurrentBattle();
        m_NPCBattleField.restart(m_battleFieldOrigin);
    }

    public void startTrainingBattle(Vector3 position)
    {
        m_isActive = true;
        m_battleFieldOrigin = position;
        NPCManager.singleton.createBattlefieldRequest(this);
    }

    public void onCreateBattlefieldDone()
    {
        NPCManager.singleton.onCreateBattlefieldDone(this);

        //m_NPCBattleTrainer.startBattle(m_battleFieldOrigin);

        Debug.Log("startTrainingBattle");
        m_NPCBattleTrainer.startTrainingBattle(m_battleFieldOrigin);
    }

    public void onBattleEnded()
    {
        m_isActive = false;
    }

}
