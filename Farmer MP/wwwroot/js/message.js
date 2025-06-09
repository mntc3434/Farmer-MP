function searchMessages() {
    let searchQuery = document.getElementById("messageSearch").value.toLowerCase();
    let users = document.querySelectorAll(".user-item");
    users.forEach(user => {
        let name = user.innerText.toLowerCase();
        user.style.display = name.includes(searchQuery) ? "block" : "none";
    });
}

function showMessages(userId) {
    const currentBuyerId = @Model.BuyerID;
    const currentFarmerId = @Model.FarmerID;

    let url = `/Message/Index?buyerId=${currentBuyerId}&farmerId=${userId}`;

    if (currentBuyerId === userId) {
        url = `/Message/Index?buyerId=${userId}&farmerId=${currentFarmerId}`;
    }

    window.location.href = url;
}


function showEditForm(messageId) {
    document.getElementById(`edit-form-${messageId}`).style.display = "block";
}

function hideEditForm(messageId) {
    document.getElementById(`edit-form-${messageId}`).style.display = "none";
}

function toggleEdit(messageId) {
    var editContainer = document.getElementById("edit-container-" + messageId);
    var normalContainer = document.getElementById("normal-container-" + messageId);

    if (editContainer.style.display === "none") {
        editContainer.style.display = "block";
        normalContainer.style.display = "none";
    } else {
        editContainer.style.display = "none";
        normalContainer.style.display = "block";
    }
}