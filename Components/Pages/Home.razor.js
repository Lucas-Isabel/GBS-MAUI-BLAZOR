export function scrollToId() {
    let id = document.getElementById('searchPlu').value;
    let element = document.getElementById(id);

    if (element) {
        // Definindo a posição do elemento na página
        const elementPosition = element.getBoundingClientRect().top + window.scrollY;
        const startPosition = window.scrollY;
        const distance = elementPosition - startPosition;
        const duration = 800; // Duração da rolagem em milissegundos
        let startTime = null;

        // Função para animação de rolagem
        function animation(currentTime) {
            if (startTime === null) startTime = currentTime;
            const timeElapsed = currentTime - startTime;
            const run = ease(timeElapsed, startPosition, distance, duration);
            window.scrollTo(0, run);

            if (timeElapsed < duration) requestAnimationFrame(animation);
        }

        // Função de easing para suavizar a animação
        function ease(t, b, c, d) {
            t /= d / 2;
            if (t < 1) return c / 2 * t * t + b;
            t--;
            return -c / 2 * (t * (t - 2) - 1) + b;
        }

        requestAnimationFrame(animation);
    } else {
        alert("Produto não cadastrado");
    }
}

var campoFiltro = document.getElementById("searchPluDescription")

if (campoFiltro) {
campoFiltro.addEventListener("input", function(){
    console.log(this.value);
    var produtos = document.querySelectorAll(".descricao-todos-produtos");
    produtos.forEach(produto => {
        let expressao = new RegExp(this.value,"i")

        let id = produto.id
        let descricao_codigo = id.split("-")
        let codigo = descricao_codigo[1]
        let descricao = produto.textContent
        let idLinhaTabela = `row-${codigo}`
        let linha = document.getElementById(idLinhaTabela)
        if(linha) {
        if (!expressao.test(descricao)){
            linha.classList.add("invisivel")
        } else {
            linha.classList.remove("invisivel")
        }
    }
    });
})
}

console.log("oi");

export function filterProductsByDescription() {
    var campoFiltro = document.getElementById("searchPluDescription");

    if (campoFiltro) {
        campoFiltro.addEventListener("input", function () {
            console.log(this.value);
            var produtos = document.querySelectorAll(".descricao-todos-produtos");
            produtos.forEach(produto => {
                let expressao = new RegExp(this.value, "i");

                let id = produto.id;
                let descricao_codigo = id.split("-");
                let codigo = descricao_codigo[1];
                let descricao = produto.textContent;
                let idLinhaTabela = `row-${codigo}`;
                let linha = document.getElementById(idLinhaTabela);
                if (linha) {
                    if (!expressao.test(descricao)) {
                        linha.classList.add("invisivel");
                    } else {
                        linha.classList.remove("invisivel");
                    }
                }
            });
        });
    }
}
