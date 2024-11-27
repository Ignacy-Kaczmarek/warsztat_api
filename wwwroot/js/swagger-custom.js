function addRoleButtons() {
    console.log("Adding role buttons...");

    // Przykładowe tokeny dla różnych ról
    const tokens = {
        "Client": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjEiLCJlbWFpbCI6ImphbkBrb3dhbHNraS5wbCIsInJvbGUiOiJDbGllbnQiLCJleHAiOjE3Mzk5MDg4OTcsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0IiwiYXVkIjoiaHR0cHM6Ly9sb2NhbGhvc3QifQ.sPAcgXRcWOFGsJP1RaeJkXI3w3t92p_l34I2HnEGOyg",
        "Employee": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjIiLCJlbWFpbCI6IndpdG9sZEBtYWtpZXdpY3oucGwiLCJyb2xlIjoiRW1wbG95ZWUiLCJleHAiOjE3Mzk5MDg5NTMsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0IiwiYXVkIjoiaHR0cHM6Ly9sb2NhbGhvc3QifQ.U2l9jDcl8V4RuCYoPpFdls0VeTse5QqLic8a_4OHM4E",
        "Manager": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjEiLCJlbWFpbCI6ImFsYmVydEBtYW5pZXJrYS5wbCIsInJvbGUiOiJNYW5hZ2VyIiwiZXhwIjoxNzM5OTA4OTI4LCJpc3MiOiJodHRwczovL2xvY2FsaG9zdCIsImF1ZCI6Imh0dHBzOi8vbG9jYWxob3N0In0.wAocpNUp7_Oj2JkP3m9jLVT09OV81nLdaWIyzRhOtB8"
    };

    // Znajdź główny element Swagger UI
    const swaggerUI = document.querySelector('.swagger-ui');
    if (!swaggerUI) {
        console.error('Swagger UI not found!');
        return;
    }

    // Utwórz kontener na przyciski
    const buttonContainer = document.createElement('div');
    buttonContainer.style.margin = "10px 0";
    buttonContainer.style.border = "1px solid #ccc";
    buttonContainer.style.padding = "10px";
    buttonContainer.style.backgroundColor = "#f9f9f9";
    buttonContainer.style.borderRadius = "5px";
    buttonContainer.style.textAlign = "center";

    // Dodaj przyciski dla każdej roli
    for (const [role, token] of Object.entries(tokens)) {
        const button = document.createElement('button');
        button.innerText = `Set ${role} Token`;
        button.style.marginRight = "10px";
        button.style.padding = "5px 10px";
        button.style.cursor = "pointer";
        button.style.backgroundColor = "#007bff";
        button.style.color = "white";
        button.style.border = "none";
        button.style.borderRadius = "5px";
        button.style.fontSize = "14px";

        // Obsługa kliknięcia przycisku
        button.onclick = () => {
            console.log(`Button clicked for role: ${role}`);

            // Znajdź obiekt authActions i wymuś autoryzację
            if (window.ui && window.ui.authActions && window.ui.authActions.authorize) {
                window.ui.authActions.authorize({
                    Bearer: {
                        name: "Bearer",
                        schema: {
                            type: "http",
                            in: "header",
                            scheme: "bearer",
                            bearerFormat: "JWT",
                        },
                        value: token,
                    },
                });
                console.log(`Token for ${role} successfully set and authorized!`);
            } else {
                console.error('Authorization function not found in Swagger UI!');
            }
        };

        buttonContainer.appendChild(button);
    }

    // Dodaj kontener przycisków na górze Swagger UI
    swaggerUI.prepend(buttonContainer);
    console.log('Button container added to the UI');
}

// Poczekaj na załadowanie Swagger UI i uruchom funkcję
const interval = setInterval(() => {
    const swaggerUI = document.querySelector('.swagger-ui');
    if (swaggerUI && window.ui) {
        clearInterval(interval);
        console.log('Swagger UI found');
        addRoleButtons();
    } else {
        console.log('Waiting for Swagger UI...');
    }
}, 500);
