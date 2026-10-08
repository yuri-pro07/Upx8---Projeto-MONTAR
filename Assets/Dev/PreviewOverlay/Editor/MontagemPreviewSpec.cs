using System;

namespace MontAR.Dev
{
    // Espelho do arquivo spec_montagem.json (lido com JsonUtility, por isso os campos públicos em snake_case).
    // Quadro da placa: placa deitada, borda traseira (I/O) à esquerda, borda superior para cima;
    // origem no canto superior esquerdo, x para a direita, y para baixo, z para cima; mm.
    // Quadro do tapete: mesma orientação, origem no canto superior esquerdo do tapete.

    [Serializable]
    public class MontagemPreviewSpec
    {
        public string convention;
        public string platform;
        public string ram_generation;
        public BoardElementSpec[] board_elements;
        public MountingHoleSpec[] mounting_holes;
        public MatSpec mat;
        public StepPreviewSpec[] steps;
        public ShotSpec[] shots;
        public string precision_note_pt;
        public string[] assumptions;
        public string[] open_questions;
        public string[] disclaimers_pt;
    }

    [Serializable]
    public class BoardElementSpec
    {
        public string id;
        public string label_pt;
        public string kind;
        public float x_mm;
        public float y_mm;
        public float w_mm;
        public float h_mm;
        public float height_mm;
        public float[] color_rgb;
        public bool metallic;
    }

    [Serializable]
    public class MountingHoleSpec
    {
        public float x_mm;
        public float y_mm;
    }

    [Serializable]
    public class MatSpec
    {
        public float width_mm;
        public float height_mm;
        public float board_left_mm;
        public float board_top_mm;
        public float marker_center_x_mm;
        public float marker_center_y_mm;
        public float marker_size_mm;
        public string[] printed_labels_pt;
        public string rationale_pt;
    }

    [Serializable]
    public class StepPreviewSpec
    {
        public string step_id;
        public float roteiro_index;
        public string title_pt;
        public string component_pt;
        public string instruction_pt;
        public string common_error_alert_pt;
        public string[] checklist_pt;
        public float target_x_mm;
        public float target_y_mm;
        public float target_z_mm;
        public OverlayElementSpec[] overlay;

        // Peça de que a etapa depende (catálogo de peças) e etapa sem marcador.
        public string part_id;
        public string model_id;
        public int part_min_qty;
        public int part_max_qty;
        public bool no_tracking;
    }

    [Serializable]
    public class OverlayElementSpec
    {
        public string id;
        public string purpose_pt;
        public string shape;
        public float size_x_mm;
        public float size_y_mm;
        public float size_z_mm;
        public float offset_x_mm;
        public float offset_y_mm;
        public float offset_z_mm;
        public float rotation_deg_about_up;
        public float[] color_rgba;
        public bool animate;
        public string animation_note;

        // Campos opcionais acrescentados à mão para o movimento: "insert", "bob" ou vazio.
        // O deslocamento usa o quadro da placa (x direita, y para baixo, z para cima; mm).
        public string motion;
        public float travel_x_mm;
        public float travel_y_mm;
        public float travel_z_mm;
        public float period_s;
        public float hold_fraction;
    }

    [Serializable]
    public class ShotSpec
    {
        public string name;
        public string step_id;
        public string description_pt;
        public float cam_x_mm;
        public float cam_y_mm;
        public float cam_z_mm;
        public float look_x_mm;
        public float look_y_mm;
        public float look_z_mm;
    }
}
