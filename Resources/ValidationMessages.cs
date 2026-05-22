namespace EmployeeManagement.Resources;

// TODO: T-07 / T-13 [課題] F-11 入力チェック [Resource: static プロパティ + ValidationMessages.resx 同名 data 要素を追加]
//        → 詳細設計書 §8 / §9.1 / §13.1 (既存プロパティは事前実装参照のため残置)
public static class ValidationMessages
{
    public static string Email_Required => "メールアドレスは必須です。";
    public static string Email_Format => "メールアドレスの形式が正しくありません。";
    public static string Email_Duplicate => "このメールアドレスは既に登録されています。";
    public static string Password_Required => "パスワードは必須です。";
    public static string Password_Policy => "パスワードは英大文字・小文字・数字・記号を含めてください。";
    public static string Birthday_Range => "生年月日は 1900/01/01 から 2100/12/31 の範囲で入力してください。";
    public static string DeptName_Required => "部署名は必須です。";
    public static string DeptName_Length => "部署名は 15 文字以内で入力してください。";
    public static string Login_Failed => "メールアドレスまたはパスワードが正しくありません。";
}
