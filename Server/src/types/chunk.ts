export interface RealmChunkDTO {
  chunkId: string;
  realmId: string;
  chunkX: number;
  chunkZ: number;
  payload: string;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ChunkStructureDTO {
  structureId: string;
  realmId: string;
  chunkId: string;
  structureType: string;
  data: string;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ChunkPlotDTO {
  plotId: string;
  realmId: string;
  chunkId: string;
  plotIdentifier: string;
  ownerUserId: string | null;
  data: string;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ChunkSnapshotDTO extends RealmChunkDTO {
  structures: ChunkStructureDTO[];
  plots: ChunkPlotDTO[];
}

export interface ChunkChangeDTO {
  changeId: string;
  realmId: string;
  chunkId: string;
  changeType: string;
  createdAt: string;
  chunk?: RealmChunkDTO | undefined;
  structures?: ChunkStructureDTO[] | undefined;
  plots?: ChunkPlotDTO[] | undefined;
}
