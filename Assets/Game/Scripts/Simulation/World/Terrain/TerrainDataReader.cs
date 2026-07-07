using UnityEngine;

namespace Simulation
{
    /// <summary>
    /// Terrain 数据读取器：从 Terrain.terrainData.treeInstances 读取树木数据，
    /// 创建 TreeData 并写入 WorldData 和 WorldGrid。
    ///
    /// 设计说明：
    ///   1. 纯数据类，不继承 MonoBehaviour，由 TerrainSystem 持有和操作
    ///   2. 只读 Terrain，不修改 Terrain（单向数据流）
    ///   3. 每棵树占 1 格，如果同一格子有多棵树，只保留第一棵（后续跳过）
    ///   4. TreeTypeId 暂用 treeInstance.prototypeIndex，Phase 2 树种系统完善后可改为配置映射
    ///   5. 世界坐标 Y 由 Terrain.SampleHeight 采样，确保与地形贴合
    /// </summary>
    public class TerrainDataReader
    {
        /// <summary>
        /// 从 Terrain 读取所有树木，注册到 WorldData 并写入 WorldGrid。
        /// </summary>
        /// <param name="terrain">Unity Terrain 组件。</param>
        /// <param name="worldData">世界权威数据层。</param>
        /// <param name="worldGrid">世界空间数据库。</param>
        /// <returns>成功注册的树木数量。</returns>
        public int ReadTreesFromTerrain(Terrain terrain, WorldData worldData, WorldGrid worldGrid)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError("[TerrainDataReader] Terrain 或 terrainData 为 null");
                return 0;
            }

            if (worldData == null || worldGrid == null)
            {
                Debug.LogError("[TerrainDataReader] WorldData 或 WorldGrid 为 null");
                return 0;
            }

            TreeInstance[] treeInstances = terrain.terrainData.treeInstances;
            if (treeInstances == null || treeInstances.Length == 0)
            {
                Debug.Log("[TerrainDataReader] Terrain 上没有树木");
                return 0;
            }

            Vector3 terrainSize = terrain.terrainData.size;
            Vector3 terrainPos = terrain.transform.position;
            int successCount = 0;
            int skipCount = 0;

            for (int i = 0; i < treeInstances.Length; i++)
            {
                ref readonly TreeInstance ti = ref treeInstances[i];

                // TreeInstance.position 是相对于 Terrain 的归一化坐标 (0~1)
                // 转换为世界坐标
                float worldX = ti.position.x * terrainSize.x + terrainPos.x;
                float worldZ = ti.position.z * terrainSize.z + terrainPos.z;
                float worldY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + terrainPos.y;

                // 转换为格子坐标
                WorldPos worldPos = new WorldPos(worldX, worldY, worldZ);
                GridPos gridPos = CoordinateUtility.WorldToGrid(worldPos);

                // 检查边界
                if (!worldGrid.IsInBounds(gridPos))
                {
                    skipCount++;
                    continue;
                }

                // 检查格子是否已有树（避免重复注册）
                Cell existingCell = worldGrid.GetCell(gridPos);
                if (existingCell.HasFlag(CellFlags.HasTree))
                {
                    skipCount++;
                    continue;
                }

                // 创建 TreeData（TreeTypeId 暂用 prototypeIndex）
                TreeData treeData = new TreeData(gridPos, ti.prototypeIndex);

                // 注册到 WorldData
                int treeId = worldData.CreateTree(treeData);

                // 写入 WorldGrid
                var cell = worldGrid.GetCell(gridPos);
                cell.TreeId = treeId;
                cell.SetFlag(CellFlags.HasTree | CellFlags.Occupied);
                worldGrid.SetCell(gridPos, cell);

                successCount++;
            }

            Debug.Log($"[TerrainDataReader] 树木同步完成：成功 {successCount}，跳过 {skipCount}，总计 {treeInstances.Length}");
            return successCount;
        }
    }
}
