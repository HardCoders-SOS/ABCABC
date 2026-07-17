using UnityEngine;

[System.Serializable]
public class DayInfo
{
    public float dayTime = 150f;
    public float nightTime = 150f;
    public float rescueTimeLimit = 10f;
    public int rescueFailureLimit = 3;

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
            $"Day Time : {dayTime}\n" +
            $"Night Time : {nightTime}\n" +
            $"Rescue Time : {rescueTimeLimit}\n" +
            $"Failure Limit : {rescueFailureLimit}\n" +
            $"Rescue : {rescueMin} ~ {rescueMax}\n" +
            $"Trash : {trashMin} ~ {trashMax}\n" +
            $"Running : {runningMin} ~ {runningMax}\n" +
            $"Distraction : {distractionEnabled} / " +
            $"{distractionFirstDelay} / " +
            $"{distractionMinRepeat} ~ {distractionMaxRepeat}";
    }
}
