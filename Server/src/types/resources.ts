export interface ResourceDelta {
  /**
   * Canonical identifier of the resource to adjust. For example "stone" or "arcane-essence".
   */
  resourceType: string;
  /**
   * Positive values represent consumption (deduction) and negative values represent refunds.
   */
  quantity: number;
}
