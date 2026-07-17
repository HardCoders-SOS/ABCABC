using UnityEngine;

[System.Serializable]
public class DayInfo
{
    public float dayTime;
    public float rescueTimeLimit = 10f;

    public float rescueMin;
    public float rescueMax;

    public float trashMin;
    public float trashMax;

    public float runningMin;
    public float runningMax;

    public bool distractionEnabled;
    public float distractionFirstDelay;
    public float distractionMinRepeat;
    public float distractionMaxRepeat;

    public override string ToString()
    {
        return
            $"Time : {dayTime}\n" +
            $"Rescue Time : {rescueTimeLimit}\n" +
            $"Rescue : {rescueMin} ~ {rescueMax}\n" +
            $"Trash : {trashMin} ~ {trashMax}\n" +
            $"Running : {runningMin} ~ {runningMax}\n" +
            $"Distraction : {distractionEnabled} / " +
            $"{distractionFirstDelay} / " +
            $"{distractionMinRepeat} ~ {distractionMaxRepeat}";
    }
}
