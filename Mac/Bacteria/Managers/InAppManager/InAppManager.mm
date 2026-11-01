//
//  InAppManager.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/30/13.
//
//

#import "InAppManager.h"
#import "StoreLayer.h"
//
#define kVitaminsId_1               @"com.antiBacteria.vitamins.1"
#define kVitaminsId_2               @"com.antiBacteria.vitamins.2"
#define kVitaminsId_3               @"com.antiBacteria.vitamins.3"
#define kVitaminsId_4               @"com.antiBacteria.vitamins.4"
//
InAppManager *_sharedInAppManager = nil;
//
@implementation StoreProductData
@synthesize productId;
@synthesize productDescription;
@synthesize price;
@end
//
@implementation InAppManager
//
@synthesize storeLayer;
@synthesize downloadedProducts;
//
+(InAppManager*)sharedInAppManager
{
    @synchronized([InAppManager class])
    {
        if(!_sharedInAppManager)
            _sharedInAppManager = [[InAppManager alloc] init];
        return _sharedInAppManager;
    }
}
//
-(id) init
{
    if((self = [super init]))
    {
        return self;
    }
    return nil;
}
//
-(void) startGettingProducts
{
    NSMutableSet *prodIDs = [[NSMutableSet alloc] init];
    [prodIDs addObject:kVitaminsId_1];
    [prodIDs addObject:kVitaminsId_2];
    [prodIDs addObject:kVitaminsId_3];
    [prodIDs addObject:kVitaminsId_4];
    SKProductsRequest *productRequest = [[SKProductsRequest alloc] initWithProductIdentifiers:prodIDs];
    productRequest.delegate = self;
    [productRequest start];
}
//
-(void) productsRequest:(SKProductsRequest *)request didReceiveResponse:(SKProductsResponse *)response
{
    //
    for (NSString *invalidProductId in response.invalidProductIdentifiers)
    {
        NSLog(@"Invalid product ID: %@" , invalidProductId);
    }
    //
    NSLog(@"response array count = %d",(int)[[response products] count]);
    for (SKProduct *product in [response products]) {
        //
        StoreProductData *storeProductData = [[StoreProductData alloc] init];
        //
        storeProductData.productId = product.productIdentifier;
        storeProductData.productDescription = product.description;
        //
        NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
        //
        [numberFormatter setFormatterBehavior:NSNumberFormatterBehavior10_4];
        [numberFormatter setNumberStyle:NSNumberFormatterCurrencyStyle];
        [numberFormatter setLocale:product.priceLocale];
        storeProductData.price = [numberFormatter stringFromNumber:product.price];
        //
        downloadedProducts.push_back(storeProductData);
        //
    }
    //
    [[NSNotificationCenter defaultCenter] postNotificationName:@"downloadedProducts" object:self];
    //
}
//
-(void) paymentQueue:(SKPaymentQueue *)queue removedTransactions:(NSArray *)transactions
{
    //reserved
}
//
-(void) paymentQueue:(SKPaymentQueue *)queue restoreCompletedTransactionsFailedWithError:(NSError *)error
{
    
}
//
-(void) paymentQueue:(SKPaymentQueue *)queue updatedDownloads:(NSArray *)downloads
{
    //reserved
}
//
-(void) paymentQueue:(SKPaymentQueue *)queue updatedTransactions:(NSArray *)transactions
{
    NSLog(@"%@",[queue description]);
    for(SKPaymentTransaction *transaction in transactions)
    {
        switch (transaction.transactionState)
        {
            case SKPaymentTransactionStatePurchasing:
            {
                NSLog(@"Buying Proces...");
            }
            break;
                //
            case SKPaymentTransactionStatePurchased:
            {
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
                 NSLog(@"SKPaymentTransactionStatePurchased");
            }
            break;
            //
            case SKPaymentTransactionStateRestored:
            {
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
                NSLog(@"SKPaymentTransactionStateRestored");
                //
            }
            break;
            //
            case SKPaymentTransactionStateFailed:
            {
                NSLog(@"Error an Cancelled");
                [storeLayer showMenu:YES];
            }
            break;
                //
            default:
                break;
        }
    }
}

-(void) paymentQueueRestoreCompletedTransactionsFinished:(SKPaymentQueue *)queue
{
    
}
//
-(void) buyProductWithIdentifier:(NSString *)identifier
{
    SKMutablePayment *payment = [[SKMutablePayment alloc] init];
    payment.productIdentifier = identifier;
    [[SKPaymentQueue defaultQueue] addTransactionObserver:self];
    [[SKPaymentQueue defaultQueue] addPayment:payment];
}
//
-(void) buyProductWithId:(NSString*)identifier
{
    [self buyProductWithIdentifier:identifier];
}
//
@end
