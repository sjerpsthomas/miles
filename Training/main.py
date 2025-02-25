import pre_train
import fine_tune

if __name__ == "__main__":
    # Fine-tune
    fine_tune.fine_tune(
        dataset_file_path="tokens/wjd_train.btokens"
    )
    
    # Pre-train
    # pre_train.pre_train(
    #     dataset_file_path="tokens/lakh_train.btokens"
    # )
