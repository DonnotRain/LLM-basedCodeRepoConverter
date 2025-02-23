using System;

namespace LLMSmartConverter.Models
{
    /// <summary>
    /// 系统级软件需求
    /// </summary>
    public class SystemRequirement
    {
        /// <summary>
        /// 需求ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 需求名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 需求详细描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 需求背景
        /// </summary>
        public string Background { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; set; }

        // /// <summary>
        // /// 创建时间
        // /// </summary>
        // public DateTime CreateTime { get; set; }

        // /// <summary>
        // /// 更新时间
        // /// </summary>
        // public DateTime UpdateTime { get; set; }

        // /// <summary>
        // /// 需求状态(0:待评审 1:已评审 2:开发中 3:已完成)
        // /// </summary>
        // public int Status { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; }
    }
}
