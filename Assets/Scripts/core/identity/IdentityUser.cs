using UnityEngine;

public interface IdentityUser {
    ParticipantId OwnerId { get; set; }

    void Use(GameObject obj) {
        OwnerId = obj.GetComponent<ParticipantIdentity>().Id;
    }
}