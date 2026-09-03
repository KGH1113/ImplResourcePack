use crate::errors::{CmdResult, CommandError};

#[derive(Debug, Clone, serde::Serialize)]
#[serde(rename_all = "camelCase")]
pub struct AutoUpdateResult {
    pub previous_version: String,
    pub updated_to: String,
    pub download_url: String,
}

#[tauri::command]
pub fn app_auto_update(_tag: String) -> CmdResult<AutoUpdateResult> {
    Err(CommandError::msg(
        "ImplDmNote update channel is not configured",
    ))
}
