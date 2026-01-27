import { ResourceId } from '../config/gameEnums';

export interface ResourceDelta {
  /**
   * Canonical identifier of the resource to adjust (ex: "resource.wood" or "resource.mana-resin").
   */
  resourceType: ResourceId;
  /**
   * Positive values represent consumption (deduction) and negative values represent refunds.
   */
  quantity: number;
}
