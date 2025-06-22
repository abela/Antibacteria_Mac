//
//  StoreManager.h
//  AntiBacteria
//
//  Created by jaba odishelashvili on 6/29/13.
//
//

#import <Foundation/Foundation.h>
#import <StoreKit/StoreKit.h>

@interface StoreManager : NSObject <SKProductsRequestDelegate, SKPaymentTransactionObserver>
{
    
}

+ (StoreManager *) sharedStoreManager;
- (void) buyProductWithIndentifier:(NSString *) identifier;

@end
