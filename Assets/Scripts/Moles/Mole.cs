﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/*
Mole abstract class. Contains the main behaviour of the mole and calls actions to be played on
different events (Enable, Disable, Pop...). These actions are to be defined in its derived
classes.
Facilitates the creation of moles with different behaviours on specific events 
(when popped -> change color ? play animation?) 
*/

public abstract class Mole : MonoBehaviour
{
    public enum MolePopAnswer {Ok, Fake, Expired, Disabled, Paused}
    protected enum States {Disabled, Enabled, Expired, Popping, Enabling, Disabling}
    protected States state = States.Disabled;
    protected bool fake = false;

    private class StateUpdateEvent: UnityEvent<bool, Mole>{};
    private StateUpdateEvent stateUpdateEvent = new StateUpdateEvent();
    private Coroutine timer;
    private float lifeTime;
    private float expiringTime;
    private int id = -1;
    private float activatedTimeLeft;
    private float expiringTimeLeft;
    private bool isPaused = false;
    private LoggerNotifier loggerNotifier;

    protected virtual void Start()
    {
        EnterState(States.Disabled);

        // Initialization of the LoggerNotifier. Here we will only raise Event, and we will use a function to pass and update 
        // certain parameters values every time we raise an event (UpdateLogNotifierGeneralValues). We don't set any starting values.
        loggerNotifier = new LoggerNotifier(UpdateLogNotifierGeneralValues, new Dictionary<string, string>(){
            {"MolePositionWorldX", "NULL"},
            {"MolePositionWorldY", "NULL"},
            {"MolePositionWorldZ", "NULL"},
            {"MolePositionLocalX", "NULL"},
            {"MolePositionLocalY", "NULL"},
            {"MolePositionLocalZ", "NULL"},
            {"MoleLifeTime", "NULL"},
            {"MoleActivatedDuration", "NULL"},
            {"MoleId", "NULL"},
            {"MoleIndexX", "NULL"},
            {"MoleIndexY", "NULL"},
            {"MoleSurfaceHitLocationX", "NULL"},
            {"MoleSurfaceHitLocationY", "NULL"}
        });
    }

    public void SetId(int newId)
    {
        id = newId;
    }

    public int GetId()
    {
        return id;
    }

    public bool IsActive()
    {
        return (state == States.Enabled || state == States.Enabling || state == States.Disabling);
    }

    public void Enable(float enabledLifeTime, float expiringDuration, bool isFake = false)
    {
        fake = isFake;
        lifeTime = enabledLifeTime;
        expiringTime = expiringDuration;
        ChangeState(States.Enabling);
    }

    public void Disable()
    {
        ChangeState(States.Disabling);
    }

    public void SetPause(bool pause)
    {
        isPaused = pause;
    }

    public void Reset()
    {
        StopAllCoroutines();
        state = States.Disabled;
        EnterState(States.Disabled);
    }

    // Pops the Mole. Returns an answer correspondind to its poping state.
    public MolePopAnswer Pop(Vector3 hitPoint)
    {
        if (isPaused) return MolePopAnswer.Paused;
        if (state != States.Enabled && state != States.Enabling && state != States.Expired) return MolePopAnswer.Disabled;

        Vector3 localHitPoint = Quaternion.AngleAxis(-transform.rotation.y,Vector3.up) * (hitPoint - transform.position);

        if (state == States.Expired)
        {
            loggerNotifier.NotifyLogger("Expired Mole Hit", new Dictionary<string, object>()
            {
                {"MoleExpiredDuration", expiringTime - expiringTimeLeft},
                {"MoleSurfaceHitLocationX", localHitPoint.x},
                {"MoleSurfaceHitLocationY", localHitPoint.y}
            });
            return MolePopAnswer.Expired;
        }

        if (!fake) 
        {
            loggerNotifier.NotifyLogger("Mole Hit", new Dictionary<string, object>()
            {
                {"MoleActivatedDuration", lifeTime - activatedTimeLeft},
                {"MoleSurfaceHitLocationX", localHitPoint.x},
                {"MoleSurfaceHitLocationY", localHitPoint.y}
            });

            ChangeState(States.Popping);
            return MolePopAnswer.Ok;
        }
        else 
        {
            loggerNotifier.NotifyLogger("Fake Mole Hit", new Dictionary<string, object>()
            {
                {"MoleActivatedDuration", lifeTime - activatedTimeLeft},
                {"MoleSurfaceHitLocationX", localHitPoint.x},
                {"MoleSurfaceHitLocationY", localHitPoint.y}
            });

            ChangeState(States.Popping);
            return MolePopAnswer.Fake;
        }
    }

    public void OnHoverEnter()
    {
        if (state != States.Enabled)
        {
            return;
        }
        PlayHoverEnter();
    }

    public void OnHoverLeave()
    {
        if (state != States.Enabled)
        {
            return;
        }
        PlayHoverLeave();
    }

    public UnityEvent<bool, Mole> GetUpdateEvent()
    {
        return stateUpdateEvent;
    }

    protected virtual void PlayEnable() {}
    protected virtual void PlayDisable() {}
    protected virtual void PlayHoverEnter() {}
    protected virtual void PlayHoverLeave() {}

    /*
    Transition states. Need to be called at the end of its override in the derived class to
    finish the transition.
    */
    protected virtual void PlayEnabling() 
    {
        ChangeState(States.Enabled);
    }
    protected virtual void PlayDisabling() 
    {
        ChangeState(States.Expired);
    }
    protected virtual void PlayPop() 
    {
        ChangeState(States.Disabled);
    }

    private void ChangeState(States newState)
    {
        if (newState == state)
        {
            return;
        }
        LeaveState(state);
        state = newState;
        EnterState(state);
    }

    // Does certain actions when leaving a state.
    private void LeaveState(States state)
    {
        switch(state)
        {
            case States.Disabled:
                break;
            case States.Enabled:
                StopCoroutine(timer);
                break;
            case States.Popping:
                break;
            case States.Enabling:
                break;
            case States.Disabling:
                break;
        }
    }

    // Does certain actions when entering a state.
    private void EnterState(States state)
    {
        switch(state)
        {
            case States.Disabled:
                PlayDisable();
                break;
            case States.Enabled:
                PlayEnable();
                break;
            case States.Popping:
                PlayPop();
                break;
            case States.Enabling:

                if (!fake) loggerNotifier.NotifyLogger("Mole Spawned");
                else loggerNotifier.NotifyLogger("Fake Mole Spawned");

                if (!fake) stateUpdateEvent.Invoke(true, this);

                timer = StartCoroutine(StartActivatedTimer(lifeTime));
                PlayEnabling();
                break;
            case States.Disabling:
                if (!fake) loggerNotifier.NotifyLogger("Mole Expired");
                else loggerNotifier.NotifyLogger("Fake Mole Expired");

                if (!fake) stateUpdateEvent.Invoke(false, this);

                PlayDisabling();
                break;
            case States.Expired:
                StartCoroutine(StartExpiringTimer(expiringTime));
                break;
        }
    }

    // IEnumerator starting the enabled timer.
    private IEnumerator StartActivatedTimer(float duration)
    {
        activatedTimeLeft = duration;
        while (activatedTimeLeft > 0)
        {
            if (!isPaused)
            {
                activatedTimeLeft -= Time.deltaTime;
            }
            yield return null;
        }

        if (state == States.Enabled)
        {
            Disable();
        }
    }

    // IEnumerator starting the expiring timer.
    private IEnumerator StartExpiringTimer(float duration)
    {
        expiringTimeLeft = duration;
        while (activatedTimeLeft > 0)
        {
            if (!isPaused)
            {
                expiringTimeLeft -= Time.deltaTime;
            }
            yield return null;
        }

        EnterState(States.Disabled);
    }

    // Function that will be called by the LoggerNotifier every time an event is raised, to automatically update
    // and pass certain parameters' values.
    private LogEventContainer UpdateLogNotifierGeneralValues()
    {
        return new LogEventContainer(new Dictionary<string, object>(){
            {"MolePositionWorldX", transform.position.x},
            {"MolePositionWorldY", transform.position.y},
            {"MolePositionWorldZ", transform.position.z},
            {"MolePositionLocalX", transform.localPosition.x},
            {"MolePositionLocalY", transform.localPosition.y},
            {"MolePositionLocalZ", transform.localPosition.z},
            {"MoleLifeTime", lifeTime},
            {"MoleId", id.ToString("0000")},
            {"MoleIndexX", (int)Mathf.Floor(id/100)},
            {"MoleIndexY", (id % 100)}   
        });
    }
}