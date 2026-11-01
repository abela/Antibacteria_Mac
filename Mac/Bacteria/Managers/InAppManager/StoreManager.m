//
//  StoreManager.m
//  AntiBacteria
//
//  Created by jaba odishelashvili on 6/29/13.
//
//

#import "StoreManager.h"

#define kVitaminsId_1               @"com.antiBacteria.vitamins.1"
#define kVitaminsId_2               @"com.antiBacteria.vitamins.2"
#define kVitaminsId_3               @"com.antiBacteria.vitamins.3"
#define kVitaminsId_4               @"com.antiBacteria.vitamins.4"

static StoreManager *_sharedStoreManager;

@implementation StoreManager

/** Create StoreManager Singleton Object */
+ (StoreManager *) sharedStoreManager
{
    @synchronized([StoreManager class])
    {
        if(_sharedStoreManager == nil)
            _sharedStoreManager = [[StoreManager alloc] init];
        return _sharedStoreManager;
    }
}

/** inizialization the singleton object */
- (id) init
{
    if( (self = [super init]) )
    {
        [[SKPaymentQueue defaultQueue] addTransactionObserver:self];
    }
    return self;
}

- (void) buyProductWithIndentifier:(NSString *) identifier
{
    NSSet             *productIndentifierSet = [NSSet setWithObject:identifier];
    SKProductsRequest *productRequest        = [[SKProductsRequest alloc] initWithProductIdentifiers:productIndentifierSet];
    productRequest.delegate                  = self;
    [productRequest start];
    [productRequest release];
}

/** recived products */
- (void) productsRequest:(SKProductsRequest *)request didReceiveResponse:(SKProductsResponse *)response
{
    NSArray *products = response.products;
    for(SKProduct *product in products)
    {
        [[SKPaymentQueue defaultQueue] addPayment:[SKPayment paymentWithProduct:product]];
    }
}

- (void)paymentQueue:(SKPaymentQueue *)queue updatedTransactions:(NSArray *)transactions
{    
    for (SKPaymentTransaction *transaction in transactions)
    {
        NSLog(@"%ld", transaction.transactionState);
        switch (transaction.transactionState)
        {
            case SKPaymentTransactionStatePurchased:
                /** Product puchased */
                
                if([transaction.payment.productIdentifier isEqualToString:kVitaminsId_1])
                {
                    
                } else  if([transaction.payment.productIdentifier isEqualToString:kVitaminsId_2]) {
                    
                } else if([transaction.payment.productIdentifier isEqualToString:kVitaminsId_3]) {
                    
                } else if([transaction.payment.productIdentifier isEqualToString:kVitaminsId_4]) {
                    
                }
                
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
                break;
            case SKPaymentTransactionStateFailed:
                /** Transaction failed */
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
                break;
            case SKPaymentTransactionStateRestored:
                /** There is not restore transaction in the game */
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
            default:
                break;
        }
    }
}

@end
