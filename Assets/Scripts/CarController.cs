﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public Vector3[] AllyRoute;
    public Vector3[] EnemyRoute;
    
    [Range(0.0f, 1.0f)][SerializeField] private float PositionSmoothValue;
    [Range(0.0f, 1.0f)][SerializeField] private float RotationSmoothValue;
    
    public delegate void OnGameOver(string Result);
    public static event OnGameOver GameOver;

    public static bool IsGameOver;

    [NonSerialized] public Vector3 OnCellPosition = new Vector3();
    
    private int OnRoutePosition;
    
    private bool OnMove;
    
    private List<string> InDistance = new List<string>();
    
    private List<Vector3> QueuePositionCurrent = new List<Vector3>();
    private List<Vector3> QueuePositionNext = new List<Vector3>();
    
    private void Start()
    {
        GameController.SetPosition += OnSetPosition;
        CharacterController.Die += OnDeath;
    }

    private void OnSetPosition()
    {
        if (!(GameController.MyPosition != 1))
        {
            StartCoroutine(Check());
        }
    }

    private void OnDeath(GameObject Character, Vector3 Position)
    {
        if (InDistance.Contains(Character.transform.parent.name))
        {
            InDistance.Remove(Character.transform.parent.name);
        }
    }

    public void SetPosition(Vector3 NextPosition)
    {
        Debug.Log("Trying to set position.");
        Debug.Log("On Move? " + OnMove);
        
        if (!(GameController.MyPosition != 1) || (!(GameController.MyPosition != 2) && !OnMove))
        {
            Debug.Log("No queue, starting to move!");
            
            StartCoroutine(Move(NextPosition));
            StartCoroutine(Rotate(OnCellPosition, NextPosition));
        }
        else
        {
            Debug.Log("Setting queue!");
            
            QueuePositionCurrent.Add(OnCellPosition);
            QueuePositionNext.Add(NextPosition);
        }

        OnCellPosition = NextPosition;
    }

    private IEnumerator Move(Vector3 NextPosition)
    {
        if (!(GameController.MyPosition != 2))
        {
            OnMove = true;
        }
        
        UiCar UiCar = GetComponentInChildren<UiCar>();
        
        UiCar.Icon.enabled = true;
        
        while (transform.position != NextPosition)
        {
            transform.position = Vector3.MoveTowards(transform.position, NextPosition, PositionSmoothValue * 10 * Time.deltaTime);
        
            yield return new WaitForEndOfFrame();
        }

        if (!(NextPosition != UiCell.FinishCell01))
        {
            switch (GameController.MyPosition)
            {
                case 1:
                    
                    GameOver?.Invoke("Win");

                    break;
                
                case 2:
                    
                    GameOver?.Invoke("Loose");

                    break;
            }

            IsGameOver = true;
        }
        else if (!(NextPosition != UiCell.FinishCell02))
        {
            switch (GameController.MyPosition)
            {
                case 1:
                    
                    GameOver?.Invoke("Loose");

                    break;
                
                case 2:
                    
                    GameOver?.Invoke("Win");

                    break;
            }

            IsGameOver = true;
        }
        else
        {
            if (!(GameController.MyPosition != 2) && (QueuePositionCurrent.Any() || QueuePositionNext.Any()))
            {
                Debug.Log("Getting something from queue!");
                Debug.Log("Current: " + QueuePositionCurrent.First());
                Debug.Log("Next: " + QueuePositionNext.First());
                
                StartCoroutine(Move(QueuePositionNext.First()));
                StartCoroutine(Rotate(QueuePositionCurrent.First(), QueuePositionNext.First()));

                QueuePositionCurrent.Remove(QueuePositionCurrent.First());
                QueuePositionNext.Remove(QueuePositionNext.First());
                
                Debug.Log("Queue length: " + QueuePositionCurrent.Count);
                
                yield break;
            }
            
            OnMove = false;

            UiCar.Icon.enabled = false;
        }

        if (!(GameController.MyPosition != 1))
        {
            StartCoroutine(Check());
        }
    }
    
    private IEnumerator Rotate(Vector3 CurrentPosition, Vector3 NextPosition)
    {
        Quaternion Rotation = Quaternion.LookRotation(NextPosition - CurrentPosition);
            
        while (transform.rotation != Rotation)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Rotation, RotationSmoothValue * 1000 * Time.deltaTime);
                
            yield return new WaitForEndOfFrame();
        }
    }
    
    private IEnumerator Check()
    {
        while (!OnMove)
        {
            CheckInDistance();

            yield return new WaitForEndOfFrame();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!(GameController.MyPosition != 1) && (other.transform.parent.name.Contains("Ally") || other.transform.parent.name.Contains("Enemy")))
        {
            InDistance.Add(other.transform.parent.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!(GameController.MyPosition != 1) && (other.transform.parent.name.Contains("Ally") || other.transform.parent.name.Contains("Enemy")))
        {
            InDistance.Remove(other.transform.parent.name);
        }
    }

    private void CheckInDistance()
    {
        if (OnMove || !InDistance.Any()) return;
        
        int CountAlly = 0;
        int CountEnemy = 0;
                
        foreach (string Name in InDistance)
        {
            if (Name.Contains("Ally"))
            {
                CountAlly = CountAlly + 1;
            }
            else if (Name.Contains("Enemy"))
            {
                CountEnemy = CountEnemy + 1;
            }
        }

        if (!(CountEnemy != 0))
        {
            ChangeData("Ally");
        }
        else if (!(CountAlly != 0))
        {
            ChangeData("Enemy");
        }
    }

    private void ChangeData(string Faction)
    {
        OnMove = true;
        
        switch (Faction)
        {
            case "Ally":
                
                OnRoutePosition = OnRoutePosition + 1;

                break;
            
            case "Enemy":
                
                OnRoutePosition = OnRoutePosition - 1;

                break;
        }
        
        if (OnRoutePosition > 0)
        {
            string X = AllyRoute[OnRoutePosition - 1].x.ToString();
            string Z = AllyRoute[OnRoutePosition - 1].z.ToString();
                    
            FB.MyData["Car"] = $"{X} : {Z}";
        }
        else if (OnRoutePosition < 0)
        {
            string X = EnemyRoute[Math.Abs(OnRoutePosition) - 1].x.ToString();
            string Z = EnemyRoute[Math.Abs(OnRoutePosition) - 1].z.ToString();
                    
            FB.MyData["Car"] = $"{X} : {Z}";
        }
        else
        {
            FB.MyData["Car"] = "0 : 0";
        }
                
        FB.SetValue();
    }
}
