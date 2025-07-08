using UnityEngine;

public class LevelGenerationManager : MonoBehaviour
{
    public int minRoomSize = 10;
    public int maxRoomSize = 80;
    public float minEnemyPercentage = 0.02f;
    public float maxEnemyPercentage = 0.4f;
    private GameObject roomParent;
    public void GenerateARoom(Vector3 position, Quaternion rotation)
    {
        roomParent = new GameObject("EmptyRoomParent");
        roomParent.transform.position = position;
        roomParent.transform.rotation = rotation;
        int roomSize = Random.Range(minRoomSize, maxRoomSize);
        int[][] floorLayout = getNewFloor(roomSize);
        int enemyCount = (int)(roomSize * ((Random.value * (maxEnemyPercentage - minEnemyPercentage)) + minEnemyPercentage));

    }

    private int[][] getNewFloor(int roomSize)
    {
        int squareLength = ((int)(Mathf.Sqrt(roomSize) + 1.5));
        int[][] layout = new int[squareLength][];
        for (int i = 0; i < layout.Length; i++)
        {
            layout[i] = new int[squareLength];
        }
        return layout;
    }
}
