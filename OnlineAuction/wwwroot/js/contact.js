document
    .getElementById("contactForm")
    .addEventListener("submit", function (e) {

        e.preventDefault();

        let isValid = true;

        const name = document.getElementById("fullName");
        const email = document.getElementById("email");
        const message = document.getElementById("message");

        document.getElementById("nameError").innerText = "";
        document.getElementById("emailError").innerText = "";
        document.getElementById("messageError").innerText = "";

        if (name.value.trim() === "") {
            document.getElementById("nameError").innerText =
                "This field is required";
            isValid = false;
        }

        if (email.value.trim() === "") {
            document.getElementById("emailError").innerText =
                "This field is required";
            isValid = false;
        }
        else {

            const regex =
                /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

            if (!regex.test(email.value)) {

                document.getElementById("emailError").innerText =
                    "Invalid email format";

                isValid = false;
            }
        }

        if (message.value.trim() === "") {
            document.getElementById("messageError").innerText =
                "This field is required";
            isValid = false;
        }

        if (isValid) {
            alert("Message sent successfully!");
        }

    });