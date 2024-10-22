export function ExitPageImport() {
    let toChange = confirm("Tem certeza que deseja sair da pagina e imterromper a importação?");
    if (toChange) {
        console.log("eu consegui");
    } else {
        window.location.href = "/import"
    }
}
export function confirmNavigation() {
    // Mostra uma caixa de confirmação personalizada
    return confirm("Você tem certeza que deseja sair desta página?");
}
