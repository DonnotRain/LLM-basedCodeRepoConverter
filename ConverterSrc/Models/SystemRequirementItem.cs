using System;

namespace LLMSmartConverter.Models
{
    /// <summary>
    /// 软件功能需求
    /// </summary>
    public class SystemRequirementItem
    {
        /// <summary>
        /// 模块需求ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 功能需求名称：如用户新增
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 功能需求详细描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 功能需求背景
        /// </summary>
        public string Background { get; set; }
        /// <summary>
        /// 功能目标
        /// </summary>
        public string Goal { get; set; }

        /// <summary>
        /// 用户范围
        /// </summary>
        public string UserScope { get; set; }

        /// <summary>
        /// 用户角色
        /// </summary>
        public string UserRole { get; set; }

        /// <summary>
        /// 用户故事
        /// </summary>
        public string UserStory { get; set; }

        /// <summary>
        /// 流程图
        /// </summary>
        public string FlowChart { get; set; }

        /// <summary>
        /// 用例图
        /// </summary>
        public string UseCaseDiagram { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 需求状态(0:待评审 1:已评审 2:开发中 3:已完成)
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; }
    }
}
